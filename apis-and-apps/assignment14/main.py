#!/usr/bin/env python3
import argparse
import os
import re
import time
import html
from urllib.parse import quote_plus

import gradio as gr
import feedparser
import requests
from openai import OpenAI


PERIODS = {
    "Today": "1d",
    "Last week": "7d",
    "Last month": "1m",
    "Last year": "1y",
}

def clean_html(text: str) -> str:
    if not text:
        return ""
    # Remove tags and collapse whitespace
    text = re.sub(r"<[^>]+>", " ", text)
    text = html.unescape(text)
    text = re.sub(r"\s+", " ", text).strip()
    return text

def fetch_news(query: str, period_label: str, max_items: int = 15):
    """Fetch Google News RSS for query limited by period."""
    when = PERIODS.get(period_label, "7d")
    q = f"{query} when:{when} sort:date"
    url = f"https://news.google.com/rss/search?q={quote_plus(q)}&hl=en-US&gl=US&ceid=US:en"
    feed = feedparser.parse(url)
    items = []
    for e in (feed.entries or [])[:max_items]:
        title = e.get("title", "").strip()
        link = e.get("link", "").strip()
        pub = e.get("published", "") or e.get("updated", "")
        summary = clean_html(e.get("summary", ""))
        source = ""
        # Try to extract publisher/source if present
        if hasattr(e, "source") and getattr(e.source, "title", None):
            source = e.source.title
        elif "source" in e and isinstance(e["source"], dict):
            source = e["source"].get("title", "")
        items.append({
            "title": title,
            "link": link,
            "published": pub,
            "source": source,
            "snippet": summary,
        })
    return items

def summarize_with_openai(model: str, query: str, period_label: str, items):
    client = OpenAI()
    if not items:
        return f"**No recent results** for “{query}” in *{period_label.lower()}*.", ""
    # Build compact context for the LLM
    bullet_context = []
    for it in items:
        line = f"- {it['title']} ({it['source'] or 'Unknown source'}; {it['published']}) — {it['snippet']}"
        bullet_context.append(line)
    context = "\n".join(bullet_context[:15])

    system = (
        "You are a precise news editor. Write a concise, neutral summary of the items provided. "
        "Surface the main developments, points of agreement/disagreement, and any clear timelines. "
        "If sources conflict, note it briefly. Avoid hype; be factual and readable."
    )
    user = (
        f"Topic: {query}\nPeriod: {period_label}\n\nNews items:\n{context}\n\n"
        "Return 1–3 short paragraphs and, if useful, a few concise bullets with key takeaways."
    )

    resp = client.chat.completions.create(
        model=model,
        temperature=1.0,
        messages=[{"role": "system", "content": system},
                  {"role": "user", "content": user}],
    )
    summary = resp.choices[0].message.content.strip()

    # Build nice link list (Markdown)
    links_md = []
    for it in items:
        t = it['title'] or it['link']
        s = f" — *{it['source']}*" if it['source'] else ""
        links_md.append(f"- [{t}]({it['link']}){s}")
    links_md = "\n".join(links_md)
    return summary, links_md

def run_search(query: str, period_label: str, model: str):
    if not query.strip():
        return "Please enter a search term.", ""
    try:
        items = fetch_news(query.strip(), period_label)
        summary, links_md = summarize_with_openai(model, query.strip(), period_label, items)
        return summary, links_md
    except Exception as e:
        return f"Error: {e}", ""

def build_app(model: str):
    with gr.Blocks(title="News Summarizer") as demo:
        gr.Markdown("## Latest News Summarizer\nEnter a topic and select a time period. The app searches Google News and summarizes with your selected OpenAI model.")
        with gr.Row():
            period = gr.Dropdown(choices=list(PERIODS.keys()), value="Last week", label="Time period", scale=1)
            query = gr.Textbox(label="Search term or category", placeholder="e.g., quantum computing, European elections, Nvidia", scale=3)
        go = gr.Button("Search & Summarize", variant="primary")
        with gr.Row():
            summary = gr.Markdown(label="Summary")
        gr.Markdown("### Sources")
        links = gr.Markdown()

        # Wire events
        go.click(fn=run_search, inputs=[query, period, gr.State(model)], outputs=[summary, links])

    return demo

def main():
    parser = argparse.ArgumentParser(description="Web app to search & summarize news (Google News RSS + OpenAI).")
    parser.add_argument("--model", default="gpt-5-mini", help="OpenAI model to use for summarization (default: gpt-5-mini).")
    parser.add_argument("--share", action="store_true", help="Launch with a public share link.")
    parser.add_argument("--server-port", type=int, default=7860, help="Port to run the server on.")
    args = parser.parse_args()

    if not os.getenv("OPENAI_API_KEY"):
        raise SystemExit("OPENAI_API_KEY is not set.")

    app = build_app(args.model)
    app.launch(share=args.share, server_port=args.server_port)

if __name__ == "__main__":
    main()
