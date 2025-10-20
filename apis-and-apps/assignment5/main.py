#!/usr/bin/env python3
import argparse
import base64
import json
import os
import sys
from pathlib import Path
from typing import Optional

from openai import OpenAI
from PIL import Image


# ---------- Helpers ----------
def encode_image_as_data_url(image_path: Path) -> str:
    """Return a data URL (base64) for a local image."""
    mime = {
        ".png": "image/png",
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".webp": "image/webp",
        ".gif": "image/gif",
        ".bmp": "image/bmp",
        ".tiff": "image/tiff",
    }.get(image_path.suffix.lower(), "application/octet-stream")
    raw = image_path.read_bytes()
    b64 = base64.b64encode(raw).decode("utf-8")
    return f"data:{mime};base64,{b64}"


def default_out_path(in_path: Path, suffix: str = "_gen") -> Path:
    stem = in_path.stem
    return in_path.with_name(f"{stem}{suffix}.png")


def ensure_rgb(image_path: Path) -> None:
    # Some formats (e.g., palette PNG) can cause issues. Re-save as RGB silently.
    try:
        with Image.open(image_path) as im:
            if im.mode not in ("RGB", "RGBA"):
                im.convert("RGB").save(image_path)
    except Exception:
        pass


# ---------- Core ----------
def describe_image(client: OpenAI, data_url: str, prompt_style: str) -> str:
    """
    Use a vision-capable chat model to describe the image.
    We keep the instruction terse and objective by default.
    """
    system_prompt = (
        "You are an expert visual describer. "
        "Describe exactly and concretely what is in the image."
    )
    user_prompt = (
        prompt_style
        if prompt_style
        else "Provide a single, complete prompt that captures the scene, style, lighting, and key details."
    )

    # Models: gpt-4o-mini is fast and vision-capable. Works well for captioning.
    # If your account has a different vision-capable model, you can change this.
    model = "gpt-4o-mini"

    resp = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[
            {"role": "system", "content": system_prompt},
            {
                "role": "user",
                "content": [
                    {"type": "text", "text": user_prompt},
                    {"type": "image_url", "image_url": {"url": data_url}},
                ],
            },
        ],
        temperature=0.4,
    )

    caption = resp.choices[0].message.content.strip()
    return caption


def generate_image(client, prompt: str, size: str) -> bytes:
    """
    Try gpt-image-1 then dall-e-3.
    Always request base64; if the API gives only a URL, download it.
    """
    import requests  # pip install requests

    last_err = None
    for model in ("gpt-image-1", "dall-e-3"):
        try:
            resp = client.images.generate(
                model=model,
                prompt=prompt,
                size=size,
                n=1,
                response_format="b64_json",  # <-- ask for base64 explicitly
            )

            if not resp or not getattr(resp, "data", None):
                raise RuntimeError(f"No data returned by {model}. Full response: {resp!r}")

            datum = resp.data[0]

            # 1) Preferred: base64 payload
            b64 = getattr(datum, "b64_json", None)
            if b64:
                import base64
                return base64.b64decode(b64)

            # 2) Fallback: URL payload (some SDKs/models may default to url)
            url = getattr(datum, "url", None)
            if url:
                r = requests.get(url, timeout=60)
                r.raise_for_status()
                return r.content

            # 3) Nothing useful → show everything we got
            raise RuntimeError(
                f"{model} did not return b64_json or url. datum={datum!r}, resp={resp!r}"
            )

        except Exception as e:
            last_err = e
            continue

    raise RuntimeError(f"Image generation failed with both models. Last error: {last_err}")


def run(
    image_path: Path,
    out_image_path: Optional[Path],
    size: str,
    prompt_style: str,
    json_output: bool,
) -> int:
    if not os.getenv("OPENAI_API_KEY"):
        print("ERROR: OPENAI_API_KEY is not set.", file=sys.stderr)
        return 2

    client = OpenAI()

    if not image_path.exists():
        print(f"ERROR: Input image not found: {image_path}", file=sys.stderr)
        return 1

    ensure_rgb(image_path)
    data_url = encode_image_as_data_url(image_path)

    # 1) Describe
    try:
        caption = describe_image(client, data_url, prompt_style)
    except Exception as e:
        print(f"ERROR: Vision captioning failed: {e}", file=sys.stderr)
        return 3

    # Always print to stdout, as required
    print(caption)

    # 2) Generate from the caption
    try:
        img_bytes = generate_image(client, caption, size=size)
    except Exception as e:
        # Still a valid run; we did print the caption. Exit code 0 but warn to stderr.
        print(f"WARNING: Image generation failed: {e}", file=sys.stderr)
        if json_output:
            print(
                json.dumps(
                    {"caption": caption, "image_generated": False},
                    ensure_ascii=False,
                )
            )
        return 0

    # Save
    out_path = out_image_path or default_out_path(image_path)
    out_path.write_bytes(img_bytes)

    if json_output:
        print(
            json.dumps(
                {"caption": caption, "image_generated": True, "output_path": str(out_path)},
                ensure_ascii=False,
            )
        )
    else:
        print(f"[saved] {out_path}", file=sys.stderr)

    return 0


# ---------- CLI ----------
def main():
    parser = argparse.ArgumentParser(
        description="Image → text (caption) → image generator using OpenAI Vision + Image API."
    )
    parser.add_argument("image", type=Path, help="Path to input image.")
    parser.add_argument(
        "-o", "--out",
        type=Path,
        default=None,
        help="Output image path (PNG). Default: <input>_gen.png",
    )
    parser.add_argument(
        "--size",
        default="1024x1024",
        choices=["256x256", "512x512", "1024x1024", "1024x1792", "1792x1024"],
        help="Generated image size (default: 1024x1024).",
    )
    parser.add_argument(
        "--style-prompt",
        default="",
        help="Optional extra instruction influencing how the caption is written (e.g., 'Focus on composition and lighting.').",
    )
    parser.add_argument(
        "--json",
        action="store_true",
        help="Also emit a JSON line with {caption, image_generated, output_path?}.",
    )

    args = parser.parse_args()
    try:
        code = run(
            image_path=args.image,
            out_image_path=args.out,
            size=args.size,
            prompt_style=args.style_prompt.strip(),
            json_output=args.json,
        )
        sys.exit(code)
    except KeyboardInterrupt:
        print("Interrupted.", file=sys.stderr)
        sys.exit(130)


if __name__ == "__main__":
    main()