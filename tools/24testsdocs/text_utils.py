"""Text utilities
=================

Robust Unicode-aware helpers for demos and small apps.

Features
--------
- :func:`normalize_text` – Apply Unicode normalization, case folding, and cleanup.
- :func:`slugify` – Create URL slugs, with optional ASCII transliteration.
- :func:`smart_truncate` – Truncate without breaking words (fallback when needed).
- :func:`levenshtein` – Edit distance with optional early stop.
- :func:`safe_filename` – Portable filename sanitization (Windows/Unix).

Design notes
------------
* Unicode first. We default to NFC/NFKC where appropriate and use
  :py:meth:`str.casefold` for language-agnostic case folding.
* Slugify can **keep non-Latin scripts** (e.g., Chinese/Arabic) or
  attempt ASCII transliteration (best-effort via ``NFKD`` + ASCII).
* Filenames avoid reserved Windows device names and invalid characters.
"""

from __future__ import annotations

import re
import unicodedata
from typing import Iterable, Optional


_CONTROL_RE = re.compile(r"[\x00-\x1f\x7f]")
_SEP_RE = re.compile(r"[\s\-_]+", re.UNICODE)
_ASCII_CLEAN_RE = re.compile(r"[^a-z0-9]+")
# Windows reserved device names (case-insensitive), see MSDN.
_RESERVED_BASENAMES = {
    "CON",
    "PRN",
    "AUX",
    "NUL",
    *(f"COM{i}" for i in range(1, 10)),
    *(f"LPT{i}" for i in range(1, 10)),
}
_INVALID_FILENAME_CHARS = r'<>:"/\\|?*'


def normalize_text(
    text: str,
    *,
    form: str = "NFKC",
    casefold: bool = True,
    strip: bool = True,
    keep_whitespace: bool = True,
) -> str:
    """Normalize and clean text.

    Parameters
    ----------
    text:
        Input string (Unicode).
    form:
        Unicode normalization form (``"NFC"``, ``"NFKC"``, etc.).
    casefold:
        Whether to apply :py:meth:`str.casefold` (better than lower()).
    strip:
        Strip leading/trailing whitespace.
    keep_whitespace:
        If ``False``, collapse internal whitespace to single spaces.

    Returns
    -------
    str
        Cleaned string.

    Examples
    --------
    >>> normalize_text("Cafe\\u0301 ")  # "Café"
    'café'
    """
    if text is None:  # pragma: no cover - defensive
        return ""

    s = unicodedata.normalize(form, text)
    s = _CONTROL_RE.sub("", s)
    if casefold:
        s = s.casefold()
    if not keep_whitespace:
        s = re.sub(r"\s+", " ", s, flags=re.UNICODE)
    if strip:
        s = s.strip()
    return s


def slugify(
    text: str,
    *,
    max_length: int = 80,
    separator: str = "-",
    transliterate: bool = True,
) -> str:
    """Create a URL slug.

    If ``transliterate`` is True, try to reduce to ASCII via NFKD+encode
    (dropping non-ASCII). Otherwise, keep any Unicode letters/digits and
    normalize separators.

    If the result is empty, return ``"untitled"``.

    Parameters
    ----------
    text:
        Source text.
    max_length:
        Maximum length (characters). Suffix is not appended.
    separator:
        Word separator.
    transliterate:
        Attempt ASCII transliteration.

    Returns
    -------
    str
        URL-safe slug.
    """
    if not text:
        return "untitled"

    raw = normalize_text(text, form="NFKC", casefold=True, strip=True)

    if transliterate:
        # Best-effort: remove diacritics and non-ASCII code points.
        asciiish = (
            unicodedata.normalize("NFKD", raw).encode("ascii", "ignore").decode("ascii")
        )
        asciiish = _ASCII_CLEAN_RE.sub(separator, asciiish)
        slug = _SEP_RE.sub(separator, asciiish)
    else:
        # Keep letters/digits from any script; replace others with separator.
        buf = []
        for ch in raw:
            if ch.isalnum():
                buf.append(ch)
            else:
                buf.append(separator)
        slug = "".join(buf)
        slug = _SEP_RE.sub(separator, slug)

    slug = slug.strip(separator)
    if not slug:
        slug = "untitled"

    if max_length > 0 and len(slug) > max_length:
        slug = slug[: max_length].rstrip(separator)
    return slug


def smart_truncate(
    text: str,
    max_chars: int,
    *,
    suffix: str = "…",
    whole_word: bool = True,
) -> str:
    """Truncate text to ``max_chars``.

    If ``whole_word`` is True, attempt to cut at whitespace. If a single word
    exceeds ``max_chars``, fall back to a hard cut.

    Parameters
    ----------
    text:
        Source text.
    max_chars:
        Maximum length **including** suffix (when applied).
    suffix:
        Suffix indicating truncation (e.g., ellipsis).
    whole_word:
        Prefer truncation at word boundaries.

    Returns
    -------
    str
    """
    if len(text) <= max_chars:
        return text

    if max_chars <= len(suffix):
        # Not enough budget; return suffix only.
        return suffix[:max_chars]

    budget = max_chars - len(suffix)
    if not whole_word:
        return text[:budget] + suffix

    # Word-aware truncation
    words = text.split()
    out = []
    total = 0
    for w in words:
        need = (1 if out else 0) + len(w)
        if total + need > budget:
            break
        if out:
            out.append(" ")
            total += 1
        out.append(w)
        total += len(w)

    if not out:
        return text[:budget] + suffix
    return "".join(out) + suffix


def levenshtein(a: str, b: str, *, max_cost: Optional[int] = None) -> int:
    """Compute Levenshtein edit distance.

    Uses a memory-efficient dynamic program with optional early stopping.

    Parameters
    ----------
    a, b:
        Input strings.
    max_cost:
        If provided, stop once distance exceeds this value and return a value
        greater than ``max_cost`` (useful for filters).

    Returns
    -------
    int
        Edit distance.
    """
    if a == b:
        return 0
    if not a:
        return len(b)
    if not b:
        return len(a)

    if len(a) < len(b):
        a, b = b, a

    prev = list(range(len(b) + 1))
    for i, ca in enumerate(a, 1):
        curr = [i]
        # Optional banded early stop heuristic.
        row_min = curr[0]
        for j, cb in enumerate(b, 1):
            ins = curr[j - 1] + 1
            dele = prev[j] + 1
            sub = prev[j - 1] + (ca != cb)
            v = min(ins, dele, sub)
            curr.append(v)
            row_min = min(row_min, v)
        prev = curr
        if max_cost is not None and row_min > max_cost:
            return row_min  # strictly > max_cost satisfies "exceeds"

    return prev[-1]


def safe_filename(
    name: str,
    *,
    replacement: str = "_",
    allow_hidden: bool = False,
    max_length: int = 255,
) -> str:
    """Sanitize a filename for cross-platform use.

    Rules (summary)
    ---------------
    * Strip control chars and ``<>:\"/\\|?*``.
    * Avoid leading dot (unless ``allow_hidden``).
    * Avoid trailing dots/spaces (Windows).
    * If base name equals a Windows device (``CON``, ``NUL`` …), append ``_``.
    * Enforce ``max_length`` characters.

    Parameters
    ----------
    name:
        Proposed filename.
    replacement:
        Character used to replace invalid characters.
    allow_hidden:
        Whether a leading dot is allowed.
    max_length:
        Maximum length in characters.

    Returns
    -------
    str
        A safe filename.
    """
    if not name:
        return "untitled"

    # Normalize & drop controls
    s = normalize_text(name, form="NFKC", casefold=False, keep_whitespace=True)
    s = _CONTROL_RE.sub("", s)

    # Replace invalid characters
    trans = {ord(c): replacement for c in _INVALID_FILENAME_CHARS}
    s = s.translate(trans)

    # Collapse whitespace
    s = re.sub(r"\s+", " ", s).strip()

    # Avoid leading dot unless allowed
    if s.startswith(".") and not allow_hidden:
        s = s.lstrip(".")
    s = s.rstrip(" .")

    if not s:
        s = "untitled"

    # Reserved device names (check base name before extension)
    base = s.split(".", 1)[0]
    if base.upper() in _RESERVED_BASENAMES:
        s = s + "_"

    # Enforce length, keeping extension if possible
    if max_length > 0 and len(s) > max_length:
        if "." in s:
            stem, ext = s.rsplit(".", 1)
            room = max_length - (len(ext) + 1)
            if room <= 0:
                s = s[: max_length]
            else:
                s = stem[:room].rstrip(" .") + "." + ext
        else:
            s = s[:max_length].rstrip(" .")

    if not s:
        s = "untitled"
    return s
