#!/usr/bin/env python3
import argparse
import os
import sys
from pathlib import Path

from openai import OpenAI

# Pure-Python Markdown parsing
from markdown_it import MarkdownIt

# PDF generation (pure pip wheel)
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import mm
from reportlab.platypus import (
    SimpleDocTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
    ListFlowable,
    ListItem,
    PageBreak,
)
from reportlab.lib import colors


SYSTEM_PROMPT = """You write scientific articles in Markdown.

Requirements:
- Language: exactly the language of the user's topic prompt.
- Structure (with headings):
  # Title
  ### Authors (placeholders if none provided)
  ## Abstract
  ## Introduction
  ## Methods
  ## Results
  ## Discussion
  ## Conclusions
  ## References
- Use subheadings (###/####) where helpful.
- Include at least one Markdown table if relevant: use pipe table syntax.
- Use APA 7th in-text citations, e.g., (Smith, 2021) or Smith (2021).
- Provide an APA 7th reference list with DOIs/URLs when possible.
- Output MUST be valid Markdown only, no code fences, no HTML."""

def generate_markdown(client: OpenAI, topic: str, model: str) -> str:
    resp = client.chat.completions.create(
        model=model,
        temperature=0.4,
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": topic.strip()},
        ],
    )
    return resp.choices[0].message.content.strip()


# ---------- Markdown → ReportLab flowables ----------

def md_to_flowables(md_text: str):
    """
    Minimal Markdown -> ReportLab flowables without external system deps.
    Supports:
      - #, ##, ###, #### headings
      - paragraphs
      - unordered (-, *) and ordered (1., 2.) lists
      - GitHub-style pipe tables:
          | H1 | H2 |
          | --- | --- |
          | c1 | c2 |
    """
    import re
    from reportlab.lib import colors
    from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
    from reportlab.platypus import Paragraph, Spacer, ListFlowable, ListItem, Table, TableStyle, PageBreak

    styles = getSampleStyleSheet()
    styles["Normal"].fontName = "Times-Roman"
    styles["Normal"].fontSize = 10.5
    styles["Normal"].leading = 14

    H1 = ParagraphStyle("H1", parent=styles["Heading1"], fontName="Times-Bold", fontSize=24, leading=28, spaceAfter=6)
    H2 = ParagraphStyle("H2", parent=styles["Heading2"], fontName="Times-Bold", fontSize=16, leading=20, spaceBefore=10, spaceAfter=6, keepWithNext=True)
    H3 = ParagraphStyle("H3", parent=styles["Heading3"], fontName="Times-Bold", fontSize=13, leading=16, spaceBefore=8, spaceAfter=4, keepWithNext=True)
    H4 = ParagraphStyle("H4", parent=styles["Heading4"], fontName="Times-Bold", fontSize=11, leading=14, spaceBefore=6, spaceAfter=3, keepWithNext=True)
    P  = styles["Normal"]

    flows = []

    lines = md_text.splitlines()

    i = 0
    para_buf = []
    list_buf = []   # list of strings
    list_ordered = False

    def flush_paragraph():
        nonlocal para_buf
        if para_buf:
            text = " ".join(s.strip() for s in para_buf).strip()
            if text:
                flows.append(Paragraph(text, P))
                flows.append(Spacer(1, 4))
            para_buf = []

    def flush_list():
        nonlocal list_buf
        if list_buf:
            flows.append(
                ListFlowable(
                    [ListItem(Paragraph(x, P)) for x in list_buf],
                    bulletType="1" if list_ordered else "bullet",
                    start="1",
                    leftIndent=12,
                    bulletFontName="Times-Roman",
                    bulletFontSize=10.5,
                )
            )
            flows.append(Spacer(1, 4))
            list_buf = []

    def is_hr(line: str) -> bool:
        return bool(re.match(r'^\s*([-*_]\s*){3,}\s*$', line))

    def is_list_item(line: str):
        m = re.match(r'^\s*([-*])\s+(.*)$', line)
        if m:
            return False, m.group(2)
        m = re.match(r'^\s*(\d+)\.\s+(.*)$', line)
        if m:
            return True, m.group(2)
        return None

    def is_heading(line: str):
        m = re.match(r'^\s*(#{1,6})\s+(.*)$', line)
        if m:
            return len(m.group(1)), m.group(2).strip()
        return None

    def looks_like_table_header(idx: int) -> bool:
        """Detects header + separator line pattern for GFM tables."""
        if idx + 1 >= len(lines):
            return False
        hdr = lines[idx].strip()
        sep = lines[idx + 1].strip()
        if '|' not in hdr or '|' not in sep:
            return False
        # simple separator: | --- | :---: | ---: |
        return bool(re.match(r'^\|?\s*:?-{3,}.*\|\s*:?-{3,}.*\|?\s*$', sep))

    def parse_table(start_idx: int):
        """Parse a GFM pipe table block from lines[start_idx..] -> (rows, next_index)."""
        header_line = lines[start_idx].strip().strip('|')
        sep_line = lines[start_idx + 1].strip()
        row_idx = start_idx + 2
        rows = []

        header_cells = [c.strip() for c in header_line.split('|')]
        rows.append(header_cells)

        while row_idx < len(lines):
            l = lines[row_idx]
            if '|' not in l:
                break
            # stop if blank line between rows
            if l.strip() == "":
                break
            row_cells = [c.strip() for c in l.strip().strip('|').split('|')]
            rows.append(row_cells)
            row_idx += 1

        # normalize row widths
        max_cols = max(len(r) for r in rows)
        norm = [r + [""] * (max_cols - len(r)) for r in rows]
        return norm, row_idx

    while i < len(lines):
        line = lines[i]

        # Blank line -> flush buffers
        if line.strip() == "":
            flush_paragraph()
            flush_list()
            i += 1
            continue

        # Horizontal rule -> flush and add spacer
        if is_hr(line):
            flush_paragraph()
            flush_list()
            flows.append(Spacer(1, 8))
            i += 1
            continue

        # Table?
        if looks_like_table_header(i):
            flush_paragraph()
            flush_list()
            data, i_next = parse_table(i)
            tbl = Table(data, hAlign="LEFT")
            tbl.setStyle(
                TableStyle([
                    ("GRID", (0, 0), (-1, -1), 0.5, colors.grey),
                    ("BACKGROUND", (0, 0), (-1, 0), colors.whitesmoke),
                    ("FONTNAME", (0, 0), (-1, 0), "Times-Bold"),
                    ("ALIGN", (0, 0), (-1, -1), "LEFT"),
                    ("VALIGN", (0, 0), (-1, -1), "TOP"),
                    ("FONTSIZE", (0, 0), (-1, -1), 9.5),
                    ("TOPPADDING", (0, 0), (-1, -1), 4),
                    ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
                ])
            )
            flows.append(tbl)
            flows.append(Spacer(1, 6))
            i = i_next
            continue

        # Heading?
        h = is_heading(line)
        if h:
            level, text = h
            flush_paragraph()
            flush_list()
            style = H1 if level == 1 else H2 if level == 2 else H3 if level == 3 else H4
            flows.append(Paragraph(text, style))
            # Put References on a new page if it’s an H2
            if level == 2 and text.strip().lower() == "references":
                flows.append(PageBreak())
            i += 1
            continue

        # List item?
        li = is_list_item(line)
        if li is not None:
            ordered, content = li
            # Starting a new list?
            if not list_buf:
                list_ordered = ordered
            # Switching between ordered/unordered -> flush previous list
            elif list_ordered != ordered:
                flush_list()
                list_ordered = ordered
            list_buf.append(content.strip())
            i += 1
            continue

        # Otherwise, accumulate a paragraph
        para_buf.append(line.rstrip())
        i += 1

    # Final flush
    flush_paragraph()
    flush_list()
    return flows


def build_pdf(md_text: str, out_pdf: Path):
    doc = SimpleDocTemplate(
        str(out_pdf),
        pagesize=A4,
        leftMargin=18 * mm,
        rightMargin=18 * mm,
        topMargin=22 * mm,
        bottomMargin=22 * mm,
        title="Paper",
        author="AutoGenerated",
    )
    story = md_to_flowables(md_text)
    doc.build(story)


def main():
    ap = argparse.ArgumentParser(description="Generate a scientific article (Markdown → PDF) without external system deps.")
    ap.add_argument("topic", help="Topic for the scientific article (language of the paper must be the same as this).")
    ap.add_argument("-o", "--out", type=Path, default=Path("paper.pdf"), help="Output PDF filename (default: paper.pdf).")
    ap.add_argument("--model", default="gpt-4o-mini", help="OpenAI model (default: gpt-4o-mini).")
    args = ap.parse_args()

    # No console output on success
    if not os.getenv("OPENAI_API_KEY"):
        sys.exit(2)

    try:
        client = OpenAI()
        md = generate_markdown(client, args.topic, args.model)
        build_pdf(md, args.out)
    except Exception:
        sys.exit(1)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        sys.exit(130)
