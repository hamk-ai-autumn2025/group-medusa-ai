import argparse
import os
import sys
import csv
import requests
from bs4 import BeautifulSoup
from docx import Document
from PyPDF2 import PdfReader
from openai import OpenAI

#!/usr/bin/env python3
"""
Command-line utility to feed one or multiple inputs (text, url, csv, docx, pdf) to OpenAI LLM.
Saves output to stdout or a file. Default behavior: summarize if no query is provided.

Dependencies:
    pip install openai requests beautifulsoup4 python-docx PyPDF2

Set OPENAI_API_KEY in your environment.
"""


# Default model; allow override
DEFAULT_MODEL = "gpt-3.5-turbo"
# Conservative chunk size in characters; tune as needed
CHUNK_SIZE = 6000


def ensure_api_key():
        key = os.getenv("OPENAI_API_KEY")
        if not key:
                sys.exit("OPENAI_API_KEY environment variable is not set.")
        # instantiate OpenAI client using the API key (new openai-python v1 interface)
        global client
        client = OpenAI(api_key=key)


def read_text_file(path):
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
                return f.read()


def read_csv_file(path, max_rows=5000):
        # convert csv to a readable text representation
        out_lines = []
        with open(path, newline="", encoding="utf-8", errors="ignore") as csvfile:
                reader = csv.reader(csvfile)
                for i, row in enumerate(reader):
                        out_lines.append(", ".join(row))
                        if i >= max_rows:
                                out_lines.append("[...truncated rows...]")
                                break
        return "\n".join(out_lines)


def read_docx_file(path):
        doc = Document(path)
        paras = [p.text for p in doc.paragraphs if p.text and p.text.strip()]
        return "\n".join(paras)


def read_pdf_file(path):
        out = []
        try:
                reader = PdfReader(path)
                for page in reader.pages:
                        try:
                                text = page.extract_text()
                        except Exception:
                                text = ""
                        if text:
                                out.append(text)
        except Exception as e:
                out.append(f"[Failed to read PDF: {e}]")
        return "\n".join(out)


def read_url(url):
        try:
                resp = requests.get(url, timeout=15)
                resp.raise_for_status()
                soup = BeautifulSoup(resp.text, "html.parser")
                # remove scripts/styles
                for s in soup(["script", "style", "noscript"]):
                        s.decompose()
                text = soup.get_text(separator="\n")
                # collapse whitespace
                lines = [l.strip() for l in text.splitlines() if l.strip()]
                return "\n".join(lines)
        except Exception as e:
                return f"[Failed to fetch URL {url}: {e}]"


def detect_and_load(path_or_url):
        if path_or_url.startswith("http://") or path_or_url.startswith("https://"):
                return f"--- BEGIN URL: {path_or_url} ---\n" + read_url(path_or_url) + f"\n--- END URL: {path_or_url} ---\n"
        if not os.path.exists(path_or_url):
                return f"[Not found: {path_or_url}]"
        ext = os.path.splitext(path_or_url)[1].lower()
        if ext in (".txt", ".md", ".py", ".json", ".yaml", ".yml", ".log"):
                data = read_text_file(path_or_url)
        elif ext in (".csv",):
                data = read_csv_file(path_or_url)
        elif ext in (".docx",):
                data = read_docx_file(path_or_url)
        elif ext in (".pdf",):
                data = read_pdf_file(path_or_url)
        else:
                # fallback to text
                try:
                        data = read_text_file(path_or_url)
                except Exception:
                        data = f"[Unsupported or unreadable file type: {path_or_url}]"
        header = f"--- BEGIN FILE: {path_or_url} ---\n"
        footer = f"\n--- END FILE: {path_or_url} ---\n"
        return header + data + footer


def chunk_text(text, max_chars=CHUNK_SIZE):
        if len(text) <= max_chars:
                return [text]
        chunks = []
        start = 0
        while start < len(text):
                end = min(start + max_chars, len(text))
                # try to cut at newline or space to avoid breaking sentences
                if end < len(text):
                        cut = text.rfind("\n", start, end)
                        if cut <= start:
                                cut = text.rfind(" ", start, end)
                        if cut <= start:
                                cut = end
                        end = cut
                chunks.append(text[start:end].strip())
                start = end
        return [c for c in chunks if c]
def call_llm(messages, model=DEFAULT_MODEL, max_tokens=1024, temperature=0.2):
        # wrapper around the new OpenAI client chat completions API
        try:
                resp = client.chat.completions.create(
                        model=model,
                        messages=messages,
                        max_tokens=max_tokens,
                        temperature=temperature,
                )
                # prefer attribute access used by the openai-python v1 client, fall back to dict-style if needed
                try:
                        return resp.choices[0].message.content
                except Exception:
                        return resp["choices"][0]["message"]["content"]
        except NameError:
                raise RuntimeError("OpenAI client not initialized; call ensure_api_key() first.")


def summarize_chunks(chunks, user_query, model):
        summaries = []
        for i, chunk in enumerate(chunks, 1):
                prompt = [
                        {"role": "system", "content": "You are a helpful assistant that summarizes content."},
                        {
                                "role": "user",
                                "content": f"Input part {i} of {len(chunks)}:\n\n{chunk}\n\nTask: {user_query}\n\nProvide a concise summary for this part. If asked to answer a question, answer based on this text."
                        },
                ]
                try:
                        s = call_llm(prompt, model=model)
                except Exception as e:
                        s = f"[LLM error summarizing chunk {i}: {e}]"
                summaries.append(s)
        return summaries


def combine_summaries(summaries, user_query, model):
        joined = "\n\n".join(f"PART {i+1} SUMMARY:\n{s}" for i, s in enumerate(summaries))
        prompt = [
                {"role": "system", "content": "You are a concise synthesizer."},
                {
                        "role": "user",
                        "content": f"The following are summaries of document parts:\n\n{joined}\n\nTask: {user_query}\n\nSynthesize a final output combining them, remove duplication, be concise and produce clear sections or bullet points as appropriate."
                },
        ]
        return call_llm(prompt, model=model)


def build_single_prompt(full_text, user_query, model):
        messages = [
                {"role": "system", "content": "You are a helpful assistant that can summarize or answer queries about provided text."},
                {"role": "user", "content": f"Input:\n\n{full_text}\n\nTask: {user_query}"},
        ]
        return call_llm(messages, model=model)


def main(argv=None):
        parser = argparse.ArgumentParser(description="Feed one or more sources to OpenAI LLM (summarize or custom query).")
        parser.add_argument("inputs", nargs="+", help="Input files or URLs (http/https). Multiple allowed.")
        parser.add_argument("-q", "--query", help="Query prompt to ask the LLM. If omitted, a concise summary is produced.")
        parser.add_argument("-o", "--out", help="Write output to file instead of stdout.")
        parser.add_argument("--model", default=DEFAULT_MODEL, help=f"OpenAI model to use (default: {DEFAULT_MODEL}).")
        parser.add_argument("--chunksize", type=int, default=CHUNK_SIZE, help="Max chars per chunk (default 6000).")
        args = parser.parse_args(argv)

        ensure_api_key()

        # Default query (summarize)
        if not args.query:
                user_query = "Summarize the content below: produce concise bullet points, key facts, and action items if present."
        else:
                user_query = args.query

        # Load all inputs
        pieces = []
        for inp in args.inputs:
                pieces.append(detect_and_load(inp))

        combined = "\n\n".join(pieces).strip()
        if not combined:
                sys.exit("No content loaded from inputs.")

        # Chunking
        chunks = chunk_text(combined, max_chars=args.chunksize)

        if len(chunks) == 1:
                try:
                        result = build_single_prompt(chunks[0], user_query, args.model)
                except Exception as e:
                        result = f"[LLM error: {e}]"
        else:
                summaries = summarize_chunks(chunks, user_query, args.model)
                try:
                        result = combine_summaries(summaries, user_query, args.model)
                except Exception as e:
                        result = f"[LLM error combining summaries: {e}]"

        # Output
        if args.out:
                try:
                        with open(args.out, "w", encoding="utf-8") as f:
                                f.write(result)
                        print(f"Result written to {args.out}")
                except Exception as e:
                        sys.exit(f"Failed to write output: {e}")
        else:
                print(result)


if __name__ == "__main__":
        main()