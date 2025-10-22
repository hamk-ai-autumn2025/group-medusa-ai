#!/usr/bin/env python3
import os, io, time, base64, tempfile, requests
from pathlib import Path
from typing import List, Tuple

import gradio as gr
from PIL import Image

# Optional deps: each backend only used if env key exists
try:
    from openai import OpenAI
except Exception:
    OpenAI = None

try:
    import replicate
except Exception:
    replicate = None


ASPECTS = {
    "1:1 (square)": (1024, 1024),
    "16:9 (landscape)": (1536, 1024),
    "4:3": (1536, 1024),
    "3:4 (portrait)": (1024, 1536),
    "9:16 (tall)": (1024, 1536),
}

def ensure_temp_png(img_bytes: bytes, stem: str = "image") -> str:
    """Write bytes to a temp .png and return path."""
    fd, p = tempfile.mkstemp(prefix=f"{stem}_", suffix=".png")
    os.close(fd)
    Path(p).write_bytes(img_bytes)
    return p

def pil_from_bytes(img_bytes: bytes) -> Image.Image:
    return Image.open(io.BytesIO(img_bytes)).convert("RGB")


# ---------------- Backends ----------------

def gen_openai(prompt: str, negative: str, width: int, height: int) -> List[Tuple[Image.Image, str]]:
    if OpenAI is None or not os.getenv("OPENAI_API_KEY"):
        raise RuntimeError("OpenAI not configured.")
    client = OpenAI()
    size = f"{width}x{height}"
    # Note: gpt-image-1 doesn’t support negative prompts; we just ignore it.
    resp = client.images.generate(
        model="gpt-image-1",
        prompt=prompt,
        size=size,
        n=1,
        # response_format omitted for compatibility (some servers reject it)
    )
    results: List[Tuple[Image.Image, str]] = []
    for d in (resp.data or []):
        if getattr(d, "b64_json", None):
            b = base64.b64decode(d.b64_json)
        elif getattr(d, "url", None):
            r = requests.get(d.url, timeout=120); r.raise_for_status()
            b = r.content
        else:
            continue
        img = pil_from_bytes(b)
        path = ensure_temp_png(b, "openai")
        results.append((img, path))
    if not results:
        raise RuntimeError("OpenAI returned no image.")
    return results

def gen_replicate(prompt: str, negative: str, width: int, height: int) -> List[Tuple[Image.Image, str]]:
    if replicate is None or not os.getenv("REPLICATE_API_TOKEN"):
        raise RuntimeError("Replicate not configured.")
    client = replicate.Client(api_token=os.environ["REPLICATE_API_TOKEN"])
    model = client.models.get("stability-ai/sdxl")
    versions = list(model.versions.list())
    if not versions:
        raise RuntimeError("No SDXL versions found.")
    version = versions[0].id

    inputs = {
        "prompt": prompt,
        "width": width,
        "height": height,
        "num_outputs": 1,
        "guidance_scale": 7.0,
        "num_inference_steps": 30,
    }
    if negative.strip():
        inputs["negative_prompt"] = negative.strip()

    pred = client.predictions.create(version=version, input=inputs)
    while pred.status not in ("succeeded", "failed", "canceled"):
        time.sleep(1.5)
        pred.reload()
    if pred.status != "succeeded":
        raise RuntimeError(f"Replicate failed: {getattr(pred, 'error', None)}")

    results: List[Tuple[Image.Image, str]] = []
    for url in pred.output or []:
        r = requests.get(url, timeout=180); r.raise_for_status()
        b = r.content
        img = pil_from_bytes(b)
        path = ensure_temp_png(b, "sdxl")
        results.append((img, path))
    if not results:
        raise RuntimeError("Replicate returned no image.")
    return results


# ---------------- Gradio app ----------------

AVAILABLE_BACKENDS = []
if os.getenv("OPENAI_API_KEY") and OpenAI is not None:
    AVAILABLE_BACKENDS.append("OpenAI · gpt-image-1")
if os.getenv("REPLICATE_API_TOKEN") and replicate is not None:
    AVAILABLE_BACKENDS.append("Replicate · SDXL")
if not AVAILABLE_BACKENDS:
    raise SystemExit("Configure OPENAI_API_KEY and/or REPLICATE_API_TOKEN to run this app.")

def generate(backend, prompt, negative, aspect):
    w, h = ASPECTS[aspect]
    warn = ""
    try:
        if backend.startswith("OpenAI"):
            if negative.strip():
                warn = "Note: OpenAI gpt-image-1 ignores negative prompts."
            pairs = gen_openai(prompt, negative, w, h)
        else:
            pairs = gen_replicate(prompt, negative, w, h)
    except Exception as e:
        return None, None, f"Error: {e}"

    images = [p[0] for p in pairs]
    files = [p[1] for p in pairs]
    return images, files, warn

with gr.Blocks(title="Image Generator") as demo:
    gr.Markdown("## Image Generator\nPick a backend, write a prompt (and optional negative prompt), choose aspect ratio, then generate. You can download the result as a file.")
    with gr.Row():
        backend = gr.Dropdown(choices=AVAILABLE_BACKENDS, value=AVAILABLE_BACKENDS[0], label="Backend")
        aspect = gr.Dropdown(choices=list(ASPECTS.keys()), value="1:1 (square)", label="Aspect ratio")
    prompt = gr.Textbox(lines=3, label="Prompt", placeholder="e.g., cozy cyberpunk café at night, neon reflections")
    negative = gr.Textbox(lines=2, label="Negative prompt (optional)", placeholder="e.g., text, watermark, logo")
    gen_btn = gr.Button("Generate", variant="primary")
    warn_md = gr.Markdown("")
    gallery = gr.Gallery(label="Preview", columns=1, height=512)
    files = gr.Files(label="Download file(s)")

    gen_btn.click(
        fn=generate,
        inputs=[backend, prompt, negative, aspect],
        outputs=[gallery, files, warn_md],
    )

if __name__ == "__main__":
    demo.launch()
