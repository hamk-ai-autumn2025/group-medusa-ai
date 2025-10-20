#!/usr/bin/env python3
import os
import sys
import argparse
import textwrap
from openai import OpenAI

def build_system_prompt(content_type: str):
    return textwrap.dedent(f"""
    You are a highly skilled creative writer and SEO specialist. Produce clear, engaging, and original {content_type} variations optimized for search engines.
    For each variation, maximize discoverability by:
      - Using many high-quality synonyms and related keyword phrases naturally.
      - Providing a short SEO title (<=60 chars), meta description (<=160 chars), and a bullet list of 8-12 SEO keyword/phrase variations.
      - Producing the main content (marketing text, meme caption, song lyrics, poem, or blog post) that is creative, distinct, and varied between versions.
    Write in a friendly persuasive tone unless the user specifies otherwise.
    Avoid repeating the same phrases across synonyms list and content; strive for varied vocabulary.
    """).strip()

def get_args():
    parser = argparse.ArgumentParser(description="Creative SEO writer using OpenAI. Produces multiple SEO-optimized variations.")
    parser.add_argument("-p", "--prompt", help="Main prompt/topic. If omitted, reads from stdin if piped; otherwise prompts.")
    parser.add_argument("-t", "--type", default="marketing materials", help="Content type.")
    parser.add_argument("-n", "--num", type=int, default=3, help="Number of variations (1–10).")
    parser.add_argument("--model", default=os.getenv("OPENAI_MODEL", "gpt-5-mini"), help="Model (default from OPENAI_MODEL or gpt-5-mini).")
    parser.add_argument("--temp", type=float, default=1.0, help="Temperature.")
    parser.add_argument("--max_completion_tokens", type=int, default=4096, help="Max tokens per completion.")
    return parser.parse_args()

def read_prompt(arg_prompt):
    if arg_prompt:
        return arg_prompt.strip()
    if not sys.stdin.isatty():
        return sys.stdin.read().strip()
    return input("Enter the topic/prompt for the content: ").strip()

def main():
    args = get_args()

    if not os.getenv("OPENAI_API_KEY"):
        print("ERROR: OPENAI_API_KEY environment variable not set.", file=sys.stderr)
        sys.exit(1)

    user_prompt = read_prompt(args.prompt)
    if not user_prompt:
        print("No prompt provided. Exiting.", file=sys.stderr)
        sys.exit(1)

    system_prompt = build_system_prompt(args.type)

    messages = [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": f"Create a {args.type} based on: {user_prompt}\n\n"
                                    "Produce one complete variation: SEO title, meta description, "
                                    "SEO keyword list, and the main content. Keep content original and varied."}
    ]

    client = OpenAI()  # uses OPENAI_API_KEY env var

    try:
        resp = client.chat.completions.create(
            model=args.model,
            messages=messages,
            temperature=args.temp,
            n=max(1, min(10, args.num)),
            max_completion_tokens=args.max_completion_tokens,
        )
    except Exception as e:
        print("API request failed:", str(e), file=sys.stderr)
        # Optionally: print detailed info if available
        # if hasattr(e, 'response') and hasattr(e.response, 'text'):
        #     print(e.response.text, file=sys.stderr)
        sys.exit(1)

    if not resp or not getattr(resp, "choices", None):
        print("No completions returned.", file=sys.stderr)
        # Optional debug:
        # print(repr(resp), file=sys.stderr)
        sys.exit(1)

    print("\n=== Creative SEO Variations ===\n")
    for i, choice in enumerate(resp.choices, start=1):
        content = (getattr(choice, "message", None) or {}).get("content") if isinstance(getattr(choice, "message", None), dict) else getattr(getattr(choice, "message", None), "content", "")
        content = (content or "").strip()
        print(f"--- Variation {i} ---")
        print(content)
        print()

if __name__ == "__main__":
    main()
