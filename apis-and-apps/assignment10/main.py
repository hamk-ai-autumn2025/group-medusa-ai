#!/usr/bin/env python3
import argparse
import json
import os
import sys
from pathlib import Path
from typing import Any, Dict

from openai import OpenAI


SYSTEM_PROMPT = """You are a dictionary generator. 
Return ONLY valid JSON, no preamble, no code fences, no comments.
Schema:
{
  "word": string,                  // the headword in the TARGET language
  "definition": string,            // concise, single-paragraph meaning in the TARGET language
  "synonyms": string[],            // 0..N synonyms in the TARGET language
  "antonyms": string[],            // 0..N antonyms in the TARGET language
  "examples": string[]             // 1..N short usage examples in the TARGET language
}
Guidelines:
- If the input word is not in the target language, translate the headword to the target.
- Keep the definition precise and neutral.
- Examples should be simple, natural sentences.
- Use plain UTF-8 characters; do not escape unless necessary for JSON validity.
- Do NOT include fields other than the schema.
"""

def generate_entry(client: OpenAI, word: str, target_language: str, model: str) -> Dict[str, Any]:
    user_prompt = (
        f"Target language: {target_language}\n"
        f"Input word: {word}\n"
        "Produce the JSON per schema."
    )

    resp = client.chat.completions.create(
        model=model,                 # vision not needed; plain text model is fine (e.g., gpt-4o-mini)
        temperature=0.2,
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": user_prompt},
        ],
    )
    raw = resp.choices[0].message.content.strip()

    # Be strict: parse JSON, and normalize minimal fields if missing.
    try:
        data = json.loads(raw)
    except Exception:
        # Try to salvage common cases (e.g., accidental code fences)
        fixed = raw.strip()
        if fixed.startswith("```"):
            fixed = fixed.strip("`")
            # remove possible "json" tag at the start
            fixed = fixed[fixed.find("{"):]
        data = json.loads(fixed)

    # enforce schema minimally
    out = {
        "word": data.get("word", "").strip(),
        "definition": data.get("definition", "").strip(),
        "synonyms": data.get("synonyms", []) or [],
        "antonyms": data.get("antonyms", []) or [],
        "examples": data.get("examples", []) or [],
    }

    # Ensure types are correct
    if not isinstance(out["synonyms"], list): out["synonyms"] = [str(out["synonyms"])]
    if not isinstance(out["antonyms"], list): out["antonyms"] = [str(out["antonyms"])]
    if not isinstance(out["examples"], list): out["examples"] = [str(out["examples"])]

    # Trim whitespace on list items
    out["synonyms"] = [str(x).strip() for x in out["synonyms"] if str(x).strip()]
    out["antonyms"] = [str(x).strip() for x in out["antonyms"] if str(x).strip()]
    out["examples"] = [str(x).strip() for x in out["examples"] if str(x).strip()]

    return out


def main():
    ap = argparse.ArgumentParser(
        description="LLM-powered dictionary entry as strict JSON (prints only JSON)."
    )
    ap.add_argument("word", help="The input word.")
    ap.add_argument("-o", "--out", type=Path, default=None, help="Write JSON to this file (prints nothing to stdout).")
    ap.add_argument("--lang", default="Finnish", help="Target language for the entry (default: Finnish).")
    ap.add_argument("--model", default="gpt-4o-mini", help="OpenAI model (default: gpt-4o-mini).")
    args = ap.parse_args()

    if not os.getenv("OPENAI_API_KEY"):
        # Must not print anything except JSON normally. On fatal config error, exit non-zero silently.
        sys.exit(2)

    client = OpenAI()
    try:
        entry = generate_entry(client, args.word, args.lang, args.model)
    except Exception:
        # If generation fails, exit non-zero silently (to not break "JSON only" contract).
        sys.exit(1)

    out_json = json.dumps(entry, ensure_ascii=False, indent=2)

    if args.out:
        try:
            args.out.write_text(out_json + "\n", encoding="utf-8")
        except Exception:
            sys.exit(1)
        # Do not print anything else
        return

    # Print ONLY the JSON
    sys.stdout.write(out_json)
    sys.stdout.write("\n")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        # stay silent to preserve "JSON only" contract
        sys.exit(130)
