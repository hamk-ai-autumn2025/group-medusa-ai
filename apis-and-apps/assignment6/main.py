#!/usr/bin/env python3
import argparse
import datetime as dt
import os
import re
import sys
import time
from pathlib import Path
from typing import List, Optional, Tuple

import requests
import replicate


def slugify(text: str, max_len: int = 60) -> str:
    text = text.strip().lower()
    text = re.sub(r"[^\w\s-]", "", text)
    text = re.sub(r"[\s-]+", "-", text).strip("-")
    return (text[:max_len] or "image").rstrip("-")


def now_stamp() -> str:
    return dt.datetime.now().strftime("%Y%m%d-%H%M%S")


def auto_filenames(prompt: str, count: int) -> List[str]:
    base = slugify(prompt)
    stamp = now_stamp()
    return [f"{base}-{stamp}-{i+1}.png" for i in range(count)]


def download(url: str, out_path: Path, timeout: int = 180) -> None:
    with requests.get(url, stream=True, timeout=timeout) as r:
        r.raise_for_status()
        with out_path.open("wb") as f:
            for chunk in r.iter_content(8192):
                if chunk:
                    f.write(chunk)


def parse_aspect(aspect: str) -> Tuple[int, int]:
    presets = {
        "1:1": (1024, 1024),
        "16:9": (1152, 648),
        "4:3": (1024, 768),
        "3:4": (768, 1024),
        "9:16": (648, 1152),
    }
    if aspect in presets:
        return presets[aspect]
    m = re.match(r"^(\d+)[xX](\d+)$", aspect)
    if not m:
        raise ValueError("Use 1:1, 16:9, 4:3, 3:4, 9:16 or WxH like 1024x768.")
    w, h = int(m.group(1)), int(m.group(2))
    if w % 8 or h % 8:
        raise ValueError("Width/height must be multiples of 8 (prefer 64) for diffusion models.")
    if max(w, h) > 1536:
        raise ValueError("Max side > 1536 is likely to fail or be slow/expensive on hosted SDXL.")
    return w, h


def run_sdxl(
    prompt: str,
    negative_prompt: Optional[str],
    width: int,
    height: int,
    num_outputs: int,
    seed: Optional[int],
    guidance_scale: float,
    steps: int,
    poll_sec: float = 2.5,
    timeout_sec: int = 600,
) -> List[str]:
    token = os.getenv("REPLICATE_API_TOKEN")
    if not token:
        raise RuntimeError("REPLICATE_API_TOKEN not set.")

    client = replicate.Client(api_token=token)

    # Get latest model version dynamically (avoids hardcoding a hash)
    model = client.models.get("stability-ai/sdxl")
    versions = list(model.versions.list())  # latest is usually first
    if not versions:
        raise RuntimeError("Could not retrieve SDXL versions from Replicate.")
    version = versions[0]  # assume newest

    inputs = {
        "prompt": prompt,
        "width": width,
        "height": height,
        "guidance_scale": guidance_scale,
        "num_inference_steps": steps,
        "num_outputs": num_outputs,
    }
    if negative_prompt:
        inputs["negative_prompt"] = negative_prompt
    if seed is not None:
        inputs["seed"] = seed

    try:
        pred = client.predictions.create(version=version.id, input=inputs)
    except replicate.exceptions.ReplicateError as e:
        # Bubble up clearer 402/401 messages
        msg = str(e)
        if "402" in msg or "Payment Required" in msg:
            raise RuntimeError("Replicate says Payment Required (402). Add billing/credits to your account.") from e
        if "401" in msg or "Unauthorized" in msg:
            raise RuntimeError("Unauthorized (401). Check REPLICATE_API_TOKEN.") from e
        raise

    t0 = time.time()
    while True:
        pred.reload()
        if pred.status in ("succeeded", "failed", "canceled"):
            break
        if time.time() - t0 > timeout_sec:
            raise TimeoutError("Generation timed out.")
        time.sleep(poll_sec)

    if pred.status != "succeeded":
        raise RuntimeError(f"Generation failed: status={pred.status}, error={getattr(pred, 'error', None)}")

    # SDXL on Replicate returns a list of hosted URLs
    outputs = pred.output or []
    if not outputs or not isinstance(outputs, list):
        raise RuntimeError(f"No outputs returned: {outputs}")
    return outputs


def main():
    ap = argparse.ArgumentParser(description="SDXL image generator via Replicate (URLs + local downloads).")
    ap.add_argument("-p", "--prompt", required=True, help="Positive prompt.")
    ap.add_argument("--negative-prompt", default="", help="Negative prompt.")
    ap.add_argument("--seed", type=int, default=None, help="Seed (int).")
    ap.add_argument("--aspect", default="1:1", help="1:1, 16:9, 4:3, 3:4, 9:16 or WxH like 1024x768.")
    ap.add_argument("-n", "--num-images", type=int, default=1, help="How many images (1..8).")
    ap.add_argument("--guidance", type=float, default=7.0, help="Classifier-free guidance scale.")
    ap.add_argument("--steps", type=int, default=30, help="Inference steps.")
    ap.add_argument("--json", action="store_true", help="Emit JSON with {urls, files}.")
    ap.add_argument("--dry-run", action="store_true", help="Print settings and exit.")
    args = ap.parse_args()

    try:
        width, height = parse_aspect(args.aspect)
    except Exception as e:
        print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(2)

    if not (1 <= args.num_images <= 8):
        print("WARN: num-images coerced to 1..8.", file=sys.stderr)
        args.num_images = max(1, min(8, args.num_images))

    print(f"[prompt]   {args.prompt}", file=sys.stderr)
    if args.negative_prompt:
        print(f"[neg]      {args.negative_prompt}", file=sys.stderr)
    if args.seed is not None:
        print(f"[seed]     {args.seed}", file=sys.stderr)
    print(f"[size]     {width}x{height}", file=sys.stderr)
    print(f"[steps]    {args.steps}", file=sys.stderr)
    print(f"[guidance] {args.guidance}", file=sys.stderr)
    print(f"[count]    {args.num_images}", file=sys.stderr)

    if args.dry_run:
        return

    try:
        urls = run_sdxl(
            prompt=args.prompt,
            negative_prompt=args.negative_prompt or None,
            width=width,
            height=height,
            num_outputs=args.num_images,
            seed=args.seed,
            guidance_scale=args.guidance,
            steps=args.steps,
        )
    except Exception as e:
        print(f"ERROR: generation failed: {e}", file=sys.stderr)
        sys.exit(1)

    print("Download URLs:")
    for u in urls:
        print(u)

    files = auto_filenames(args.prompt, len(urls))
    for u, name in zip(urls, files):
        try:
            download(u, Path(name))
        except Exception as e:
            print(f"WARN: failed to download {u}: {e}", file=sys.stderr)

    print("\nSaved files:")
    for name in files:
        print(name)

    if args.json:
        import json
        print(json.dumps({"urls": urls, "files": files}, ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("Interrupted.", file=sys.stderr)
        sys.exit(130)
