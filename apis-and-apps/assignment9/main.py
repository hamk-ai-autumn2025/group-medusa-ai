#!/usr/bin/env python3
import argparse
import base64
import os
import sys
from pathlib import Path
from typing import List, Dict, Any

from openai import OpenAI


MIME_BY_EXT = {
    ".png": "image/png",
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".webp": "image/webp",
    ".gif": "image/gif",
    ".bmp": "image/bmp",
    ".tiff": "image/tiff",
}


def to_data_url(p: Path) -> str:
    ext = p.suffix.lower()
    mime = MIME_BY_EXT.get(ext, "application/octet-stream")
    b64 = base64.b64encode(p.read_bytes()).decode("utf-8")
    return f"data:{mime};base64,{b64}"


def gen_for_image(client: OpenAI, img_path: Path, user_hint: str, locale: str, model: str) -> Dict[str, Any]:
    """
    Ask the model to produce a concise product description + 3 bullets + 1-2 short slogans.
    We request a strict plain-text template to keep parsing trivial.
    """
    data_url = to_data_url(img_path)

    system_msg = (
        "You are a precise product copywriter. "
        f"Write in {locale} only. Keep it factual, concise, and benefit-forward."
    )
    user_text = (
        "Analyze this product image and produce:\n"
        "1) Product Name (<= 8 words)\n"
        "2) Short Description (2–3 sentences)\n"
        "3) 3 Key Bullets (value/benefits)\n"
        "4) 1–2 Marketing Slogans (<= 10 words each)\n"
        "\n"
        "Formatting (plain text, no extra commentary):\n"
        "Product: <name>\n"
        "Description: <2-3 sentences>\n"
        "Bullets:\n"
        "- <bullet 1>\n"
        "- <bullet 2>\n"
        "- <bullet 3>\n"
        "Slogans:\n"
        "- <slogan 1>\n"
        "- <slogan 2 (optional)>\n"
    )
    if user_hint.strip():
        user_text += f"\nAdditional context from user (higher priority than guesses): {user_hint.strip()}\n"

    resp = client.chat.completions.create(
        model=model,  # e.g., "gpt-4o-mini"
        temperature=0.5,
        messages=[
            {"role": "system", "content": system_msg},
            {
                "role": "user",
                "content": [
                    {"type": "text", "text": user_text},
                    {"type": "image_url", "image_url": {"url": data_url}},
                ],
            },
        ],
    )

    content = resp.choices[0].message.content.strip()
    return {"path": str(img_path), "text": content}


def main():
    ap = argparse.ArgumentParser(
        description="Generate product descriptions + marketing slogans from 1..N images, with optional user hint."
    )
    ap.add_argument("images", nargs="+", type=Path, help="Path(s) to product image(s).")
    ap.add_argument("-o", "--out", type=Path, default=None, help="Write plain text output to this file instead of stdout.")
    ap.add_argument("--hint", default="", help="Extra user input to improve accuracy (brand, specs, model, etc.).")
    ap.add_argument("--locale", default="English", help="Output language (e.g., English, Finnish, French, German).")
    ap.add_argument("--model", default="gpt-4o-mini", help="OpenAI vision-capable model (default: gpt-4o-mini).")
    args = ap.parse_args()

    if not os.getenv("OPENAI_API_KEY"):
        print("ERROR: OPENAI_API_KEY is not set.", file=sys.stderr)
        sys.exit(2)

    # Validate files exist
    missing = [p for p in args.images if not p.exists()]
    if missing:
        for p in missing:
            print(f"ERROR: file not found: {p}", file=sys.stderr)
        sys.exit(1)

    client = OpenAI()

    results: List[Dict[str, Any]] = []
    for img in args.images:
        try:
            results.append(gen_for_image(client, img, args.hint, args.locale, args.model))
        except Exception as e:
            results.append({"path": str(img), "text": f"[ERROR processing {img.name}: {e}]"})
            continue

    # Assemble plain text report
    lines: List[str] = []
    for r in results:
        lines.append(f"=== {r['path']} ===")
        lines.append(r["text"])
        lines.append("")  # blank line between entries
    output_text = "\n".join(lines).strip() + "\n"

    if args.out:
        try:
            args.out.write_text(output_text, encoding="utf-8")
            print(f"[saved] {args.out}")
        except Exception as e:
            print(f"ERROR: could not write to {args.out}: {e}", file=sys.stderr)
            sys.exit(1)
    else:
        print(output_text)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        sys.exit(130)
