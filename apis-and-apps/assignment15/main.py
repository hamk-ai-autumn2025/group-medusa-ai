#!/usr/bin/env python3
import argparse
import json
import os
import re
import sys
from urllib.parse import urlparse

import requests
from selectolax.parser import HTMLParser

# OpenAI (pip package: openai>=1.0)
try:
    from openai import OpenAI
except Exception:
    OpenAI = None


# ----------- Utils -----------

def fetch_html(url: str, timeout: int = 25) -> str:
    headers = {
        "User-Agent": (
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
            "AppleWebKit/537.36 (KHTML, like Gecko) "
            "Chrome/124.0 Safari/537.36"
        ),
        "Accept-Language": "en;q=0.9",
        "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
    }
    r = requests.get(url, headers=headers, timeout=timeout)
    r.raise_for_status()
    return r.text


def clean_text(s: str) -> str:
    if not s:
        return ""
    s = re.sub(r"\s+", " ", s)
    return s.strip(" \n\t\r:").strip()


def first_text(node, default=""):
    return clean_text(node.text()) if node else default


def find_first(tree: HTMLParser, selectors):
    for css in selectors:
        node = tree.css_first(css)
        if node:
            txt = first_text(node)
            if txt:
                return txt
    return ""


def find_meta(tree: HTMLParser, names_or_props):
    for key, val in names_or_props:
        for n in tree.css(f"meta[{key}='{val}']"):
            content = clean_text(n.attributes.get("content", ""))
            if content:
                return content
    return ""


def parse_price(text: str) -> str:
    """
    Return a normalized price string if possible (keep currency symbol if present).
    Examples it can catch: $19.99, 19,99 €, EUR 19.99, 19.99 USD
    """
    if not text:
        return ""
    # Collapse spaces in currency like "€ 19,99"
    t = clean_text(text)
    # Common patterns with currency symbol before/after
    m = re.search(r"(?i)([$€£¥]|usd|eur|gbp|jpy)\s*([0-9]+(?:[.,][0-9]{2})?)", t)
    if m:
        return clean_text(m.group(0))
    m = re.search(r"([0-9]+(?:[.,][0-9]{2})?)\s*(?i:usd|eur|gbp|jpy|[$€£¥])", t)
    if m:
        return clean_text(m.group(0))
    # As a fallback, any number with 2 decimals
    m = re.search(r"[0-9]+(?:[.,][0-9]{2})", t)
    return clean_text(m.group(0)) if m else ""


def parse_rating(text: str) -> str:
    """
    Extract a star/score rating; prefer 0-5 scale if present.
    Examples: "4.5 out of 5", "Rating: 4.2/5", "4.7 stars", "Score 92/100"
    """
    if not text:
        return ""
    t = clean_text(text)
    # x out of y
    m = re.search(r"([0-9]+(?:\.[0-9]+)?)\s*(?:out of|/)\s*([0-9]+(?:\.[0-9]+)?)", t, re.IGNORECASE)
    if m:
        return f"{m.group(1)}/{m.group(2)}"
    # x.y stars
    m = re.search(r"([0-9]+(?:\.[0-9]+)?)\s*stars?", t, re.IGNORECASE)
    if m:
        return f"{m.group(1)}/5"
    # percentage or /100
    m = re.search(r"([0-9]{1,3})(?:/100|%)", t)
    if m:
        try:
            score = float(m.group(1))
            return f"{round(score/20, 2)}/5"  # map 0..100 to 0..5
        except Exception:
            pass
    # fallback: any decimal like 4.6
    m = re.search(r"\b([0-9]+(?:\.[0-9]+)?)\b", t)
    return f"{m.group(1)}/5" if m else ""


# ----------- Extraction -----------

def extract_fields(url: str, html: str):
    tree = HTMLParser(html)
    host = urlparse(url).netloc.lower()

    # Title / Name selectors
    name_selectors = [
        "h1.product-title",
        "h1#productTitle",                   # Amazon
        "span#productTitle",                 # Amazon alt
        "h1.product-name",
        "div.sku-title h1",                  # BestBuy style
        "h1",
    ]
    # Try OG/Schema meta too
    name = find_first(tree, name_selectors) or find_meta(tree, [
        ("property", "og:title"),
        ("name", "title"),
        ("itemprop", "name"),
        ("name", "twitter:title"),
    ])

    # Price selectors
    price_selectors = [
        "span.a-price > span.a-offscreen",   # Amazon
        "span#priceblock_ourprice",          # Amazon legacy
        "span#priceblock_dealprice",         # Amazon deal
        ".price .amount",
        ".product-price",
        "span.price",
        "meta[itemprop='price']",
    ]
    price_raw = find_first(tree, price_selectors)
    if not price_raw:
        price_raw = find_meta(tree, [
            ("property", "product:price:amount"),
            ("itemprop", "price"),
        ])
    price = parse_price(price_raw)

    # Rating selectors
    rating_selectors = [
        "span#acrPopover",                   # Amazon badge
        "span[data-hook='rating-out-of-text']",
        "span.review-rating",
        "span[itemprop='ratingValue']",
        ".rating .value",
        ".average-rating",
    ]
    rating_raw = find_first(tree, rating_selectors)
    if not rating_raw:
        rating_raw = find_meta(tree, [
            ("itemprop", "ratingValue"),
            ("name", "rating"),
        ])
    rating = parse_rating(rating_raw)

    # Description selectors (prefer a concise feature block; fallback meta description)
    desc_selectors = [
        "#feature-bullets",                  # Amazon bullets container
        "#productDescription",
        ".product-description",
        "div#productDetails_techSpec_section_1",
        ".a-expander-content",
        ".description",
        "article.product",                   # generic
    ]
    description = ""
    for css in desc_selectors:
        n = tree.css_first(css)
        if n:
            txt = clean_text(n.text(separator=" "))
            if txt and len(txt) > 60:
                description = txt
                break
    if not description:
        description = find_meta(tree, [
            ("name", "description"),
            ("property", "og:description"),
            ("name", "twitter:description"),
        ])

    # AliExpress quirks (just a couple of helpful extras)
    if "aliexpress" in host:
        if not name:
            name = find_first(tree, ["h1.product-title-text"]) or name
        if not price:
            price = parse_price(find_first(tree, [".product-price-value", ".product-price-current"]))
        if not rating:
            rating = parse_rating(find_first(tree, [".overview-rating-average", ".product-reviewer-rating"]))

    return {
        "name": name,
        "price": price,
        "rating": rating,
        "description": description,
    }


# ----------- LLM rewrite -----------

def rewrite_description(model: str, fields: dict) -> str:
    """
    Use OpenAI to improve the description, factoring in price and rating.
    Keeps the language close to the original when possible.
    """
    if not OpenAI or not os.getenv("OPENAI_API_KEY"):
        return ""  # allow running without LLM, if desired

    client = OpenAI()

    system = (
        "You are a precise product copywriter. Improve clarity and persuasiveness "
        "without exaggeration. Keep it factual, highlight value given the price and rating, "
        "and keep the language the same as the original when possible. 120–180 words."
    )
    user = (
        f"Product name: {fields.get('name','').strip() or 'Unknown'}\n"
        f"Price: {fields.get('price','').strip() or 'Unknown'}\n"
        f"Rating: {fields.get('rating','').strip() or 'Unknown'}\n\n"
        f"Original description:\n{fields.get('description','').strip() or '(no description)'}"
    )

    resp = client.chat.completions.create(
        model=model,
        temperature=1.0,
        messages=[
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
    )
    return resp.choices[0].message.content.strip()


# ----------- CLI -----------

def main():
    ap = argparse.ArgumentParser(
        description="Scrape product fields from an e-commerce URL, then rewrite description with OpenAI, printing JSON."
    )
    ap.add_argument("url", help="Product URL (Amazon, AliExpress, etc.)")
    ap.add_argument("--model", default="gpt-5-mini", help="OpenAI model to use for rewriting (default: gpt-5-mini)")
    ap.add_argument("--timeout", type=int, default=25, help="HTTP timeout in seconds (default: 25)")
    ap.add_argument("--no-llm", dest="no_llm", action="store_true",
                help="Skip LLM rewrite and output only scraped fields")
    args = ap.parse_args()

    try:
        html = fetch_html(args.url, timeout=args.timeout)
        fields = extract_fields(args.url, html)
    except Exception as e:
        # Print an error JSON so the tool still returns machine-readable output
        print(json.dumps({"error": str(e), "url": args.url}, ensure_ascii=False, indent=2))
        sys.exit(1)

    improved = ""
    if not args.no_llm:
        try:
            improved = rewrite_description(args.model, fields)
        except Exception as e:
            improved = f"[LLM rewrite failed: {e}]"

    out = {
        "url": args.url,
        "name": fields.get("name", ""),
        "price": fields.get("price", ""),
        "rating": fields.get("rating", ""),
        "description": fields.get("description", ""),
        "improved_description": improved,
    }

    print(json.dumps(out, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        sys.exit(130)
