#!/usr/bin/env python3
import argparse
import os
import re
import sys
import time
from pathlib import Path
from typing import List, Optional, Tuple

import numpy as np
import requests
import sounddevice as sd
import soundfile as sf

# --- Optional deps (lazy import for friendlier errors) ---
try:
    from openai import OpenAI
except Exception:
    OpenAI = None

try:
    import replicate
except Exception:
    replicate = None

import base64

def save_b64_png(b64_str: str, out_path: Path):
    out_path.write_bytes(base64.b64decode(b64_str))

# ------------- Audio I/O -------------

def record_wav(path: Path, duration_s: float, samplerate: int = 16000):
    print(f"[rec] Recording {duration_s:.2f}s @ {samplerate} Hz…", file=sys.stderr)
    audio = sd.rec(int(duration_s * samplerate), samplerate=samplerate, channels=1, dtype="float32")
    sd.wait()
    audio_i16 = np.int16(np.clip(audio, -1.0, 1.0) * 32767)
    sf.write(str(path), audio_i16, samplerate, subtype="PCM_16")
    print(f"[rec] Saved: {path}", file=sys.stderr)


def speak_wav_with_openai(client, text: str, voice: str = "alloy") -> float:
    """
    Generate TTS as WAV bytes (OpenAI) and play them in-memory using sounddevice.
    Returns the measured playback duration in seconds.
    """
    import io
    import time
    import sounddevice as sd
    import soundfile as sf

    # Get WAV bytes (no temp files, no simpleaudio)
    resp = client.audio.speech.create(
        model="tts-1",           # or "gpt-4o-mini-tts"
        voice=voice,
        input=text,
        response_format="wav",   # IMPORTANT: use response_format (not 'format')
    )
    wav_bytes = resp.read()

    # Decode WAV to PCM array + samplerate in-memory
    with io.BytesIO(wav_bytes) as bio:
        audio, sr = sf.read(bio, dtype="float32", always_2d=False)  # audio: np.ndarray

    # Ensure mono or stereo is handled safely
    # sounddevice can play 1D (mono) or 2D (frames, channels); our read above fits both.

    t0 = time.perf_counter()
    sd.play(audio, sr, blocking=True)  # block until done; returns reliably
    return time.perf_counter() - t0


# ------------- Helpers -------------

def parse_aspect_ratio(ar: str) -> Tuple[int, int]:
    presets = {
        "1:1": (1024, 1024),
        "16:9": (1536, 1024),
        "4:3": (1024, 1536),
        "3:4": (1536, 1024),
        "9:16": (1024, 1536),
    }
    if ar in presets:
        return presets[ar]
    m = re.match(r"^(\d+)[xX](\d+)$", ar)
    if m:
        w, h = int(m.group(1)), int(m.group(2))
        if w % 8 or h % 8:
            raise ValueError("Width/height must be multiples of 8 (prefer 64).")
        if max(w, h) > 1536:
            raise ValueError("Max side > 1536 is likely to fail/slow on hosted SDXL.")
        return w, h
    raise ValueError("Use 1:1, 16:9, 4:3, 3:4, 9:16 or WxH like 1024x768.")


def slugify(text: str, max_len: int = 60) -> str:
    text = text.strip().lower()
    text = re.sub(r"[^\w\s-]", "", text)
    text = re.sub(r"[\s-]+", "-", text).strip("-")
    return (text[:max_len] or "image").rstrip("-")


def auto_filenames(prompt: str, count: int) -> List[str]:
    import datetime as dt
    stamp = dt.datetime.now().strftime("%Y%m%d-%H%M%S")
    base = slugify(prompt)
    return [f"{base}-{stamp}-{i+1}.png" for i in range(count)]


def download(url: str, out_path: Path, timeout: int = 180):
    with requests.get(url, stream=True, timeout=timeout) as r:
        r.raise_for_status()
        with out_path.open("wb") as f:
            for chunk in r.iter_content(8192):
                if chunk:
                    f.write(chunk)


# ------------- OpenAI backend -------------

def openai_transcribe(client, wav_path: Path) -> str:
    with wav_path.open("rb") as f:
        resp = client.audio.transcriptions.create(
            model="whisper-1",
            file=f,
            response_format="text",
        )
    return resp.strip()


def openai_generate(client, prompt: str, width: int, height: int, n: int):
    """
    Generate images via OpenAI. Returns a list of dicts:
      {"kind": "url", "value": "<https url>"}  or  {"kind": "b64", "value": "<base64>"}
    We omit response_format to support older deployments that reject it.
    """
    size = f"{width}x{height}"
    resp = client.images.generate(
        model="gpt-image-1",
        prompt=prompt,
        n=n,
        size=size,
        # no response_format here (some backends 400 on it)
    )

    results = []
    for d in (resp.data or []):
        u = getattr(d, "url", None)
        if u:
            results.append({"kind": "url", "value": u})
            continue
        b64 = getattr(d, "b64_json", None)
        if b64:
            results.append({"kind": "b64", "value": b64})
    if not results:
        raise RuntimeError(f"OpenAI returned no data. Full response: {resp}")
    return results


# ------------- Replicate backend (SDXL) -------------

def replicate_generate_urls(prompt: str, negative_prompt: Optional[str],
                            width: int, height: int, n: int,
                            seed: Optional[int], guidance: float, steps: int) -> List[str]:
    if replicate is None:
        raise RuntimeError("replicate package not installed. `pip install replicate`")
    token = os.getenv("REPLICATE_API_TOKEN")
    if not token:
        raise RuntimeError("REPLICATE_API_TOKEN not set.")

    client = replicate.Client(api_token=token)
    model = client.models.get("stability-ai/sdxl")
    versions = list(model.versions.list())
    if not versions:
        raise RuntimeError("Could not get SDXL versions.")
    version = versions[0]

    inputs = {
        "prompt": prompt,
        "width": width,
        "height": height,
        "num_outputs": n,
        "guidance_scale": guidance,
        "num_inference_steps": steps,
    }
    if negative_prompt:
        inputs["negative_prompt"] = negative_prompt
    if seed is not None:
        inputs["seed"] = seed

    pred = client.predictions.create(version=version.id, input=inputs)
    while pred.status not in ("succeeded", "failed", "canceled"):
        time.sleep(2.0)
        pred.reload()
    if pred.status != "succeeded":
        raise RuntimeError(f"Replicate generation failed: status={pred.status}, error={getattr(pred, 'error', None)}")
    outputs = pred.output or []
    if not outputs:
        raise RuntimeError("Replicate returned no outputs.")
    return outputs  # list of URLs


# ------------- Main -------------

def main():
    ap = argparse.ArgumentParser(description="Voice-controlled AI image generator (speak prompt → generate → download).")
    ap.add_argument("--backend", choices=["openai", "replicate"], default="openai",
                    help="Image backend (default: openai).")
    ap.add_argument("--duration", type=float, default=5.0, help="Recording duration seconds (default 5).")
    ap.add_argument("--aspect", default="1:1", help="1:1, 16:9, 4:3, 3:4, 9:16 or WxH (1024x768).")
    ap.add_argument("-n", "--num-images", type=int, default=1, help="How many images (1..8).")
    ap.add_argument("--negative-prompt", default="", help="[replicate] Negative prompt.")
    ap.add_argument("--seed", type=int, default=None, help="[replicate] Seed.")
    ap.add_argument("--guidance", type=float, default=7.0, help="[replicate] Guidance scale.")
    ap.add_argument("--steps", type=int, default=30, help="[replicate] Steps.")
    ap.add_argument("--voice", default="alloy", help="OpenAI TTS voice for confirmations.")
    ap.add_argument("--speak", action="store_true", help="Speak back the recognized prompt and completion.")
    args = ap.parse_args()

    # Env checks
    if args.backend == "openai":
        if OpenAI is None:
            print("ERROR: openai package missing. `pip install openai`", file=sys.stderr)
            sys.exit(2)
        if not os.getenv("OPENAI_API_KEY"):
            print("ERROR: OPENAI_API_KEY not set.", file=sys.stderr)
            sys.exit(2)
    else:
        if not os.getenv("REPLICATE_API_TOKEN"):
            print("ERROR: REPLICATE_API_TOKEN not set.", file=sys.stderr)
            sys.exit(2)
        # For TTS we still need OpenAI if --speak
        if args.speak and (OpenAI is None or not os.getenv("OPENAI_API_KEY")):
            print("ERROR: --speak requires OpenAI TTS (set OPENAI_API_KEY).", file=sys.stderr)
            sys.exit(2)

    # Record
    tmp = Path("prompt.wav")
    record_wav(tmp, args.duration)

    # Transcribe (OpenAI Whisper)
    client = OpenAI() if OpenAI is not None and os.getenv("OPENAI_API_KEY") else None
    if client is None:
        print("ERROR: OpenAI client required for transcription.", file=sys.stderr)
        sys.exit(2)

    prompt_text = openai_transcribe(client, tmp)
    print(f"[prompt] {prompt_text}")

    # Optional spoken confirmation
    if args.speak:
        try:
            speak_wav_with_openai(client, f"You said: {prompt_text}. Generating images now.", voice=args.voice)
        except Exception as e:
            print(f"WARN: TTS confirmation failed: {e}", file=sys.stderr)

    # Aspect
    try:
        width, height = parse_aspect_ratio(args.aspect)
    except Exception as e:
        print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(2)

    # Generate
    try:
        if args.backend == "openai":
            results = openai_generate(client, prompt_text, width, height, max(1, min(8, args.num_images)))
            # Split into URLs vs base64
            urls = [r["value"] for r in results if r["kind"] == "url"]
            b64s = [r["value"] for r in results if r["kind"] == "b64"]
        else:
            urls = replicate_generate_urls(
                prompt=prompt_text,
                negative_prompt=(args.negative_prompt or None),
                width=width, height=height,
                n=max(1, min(8, args.num_images)),
                seed=args.seed, guidance=args.guidance, steps=args.steps,
            )
            b64s = []
    except Exception as e:
        print(f"ERROR: generation failed: {e}", file=sys.stderr)
        sys.exit(1)

    # Print URLs
    if urls:
        print("\nDownload URLs:")
        for u in urls:
            print(u)

    # Save all outputs to files (URLs via HTTP download; base64 via decode)
    files = auto_filenames(prompt_text, len(urls) + len(b64s))
    fi = 0

    # Save from URLs
    for u in urls:
        try:
            download(u, Path(files[fi]))
        except Exception as e:
            print(f"WARN: failed to download {u}: {e}", file=sys.stderr)
        fi += 1

    # Save from base64
    for b in b64s:
        try:
            save_b64_png(b, Path(files[fi]))
        except Exception as e:
            print(f"WARN: failed to save base64 image: {e}", file=sys.stderr)
        fi += 1

    print("\nSaved files:")
    for name in files:
        print(name)


    # cleanup
    try:
        tmp.unlink(missing_ok=True)
    except Exception:
        pass


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("Interrupted.", file=sys.stderr)
        sys.exit(130)
