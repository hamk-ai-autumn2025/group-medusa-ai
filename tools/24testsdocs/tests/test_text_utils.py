import math
import random
import string
import unittest

from text_utils import (
    levenshtein,
    normalize_text,
    safe_filename,
    slugify,
    smart_truncate,
)


class TestNormalize(unittest.TestCase):
    def test_casefold_and_nfkc(self):
        self.assertEqual(normalize_text("Straße"), "straße")
        # Combining accent vs precomposed
        self.assertEqual(normalize_text("Cafe\u0301 "), "café")

    def test_strip_and_collapse(self):
        s = normalize_text("  A\tB  C  ", keep_whitespace=False)
        self.assertEqual(s, "a b c")

    def test_control_chars_removed(self):
        s = normalize_text("ok\x00ok\n", keep_whitespace=True)
        self.assertEqual(s, "okok")

    def test_empty_and_none(self):
        self.assertEqual(normalize_text("", strip=True), "")
        # None returns empty string (defensive)
        self.assertEqual(normalize_text(None), "")  # type: ignore[arg-type]


class TestSlugify(unittest.TestCase):
    def test_ascii_transliteration(self):
        self.assertEqual(slugify("Café crème!"), "cafe-creme")
        # Chinese will drop to empty -> "untitled"
        self.assertEqual(slugify("你好，世界", transliterate=True), "untitled")

    def test_unicode_slug(self):
        # Keep Arabic letters/digits
        s = slugify("مرحبا بالعالم 2025", transliterate=False)
        self.assertTrue(s.startswith("مرحبا-بالعالم-2025"))

    def test_length_and_separators(self):
        txt = "Hello---world__ again   here!!"
        self.assertEqual(slugify(txt, max_length=20), "hello-world-again")

    def test_very_long_input(self):
        long = "a" * 10000
        self.assertEqual(slugify(long, max_length=80), "a" * 80)


class TestSmartTruncate(unittest.TestCase):
    def test_word_preserving(self):
        s = smart_truncate("one two three", 9)  # budget includes suffix
        self.assertEqual(s, "one two…")

    def test_hard_cut_when_single_word(self):
        s = smart_truncate("supercalifragilistic", 8)
        self.assertEqual(s, "superc…")

    def test_tiny_budget(self):
        self.assertEqual(smart_truncate("hello", 1), "…")
        self.assertEqual(smart_truncate("hello", 2), "…")

    def test_unicode_emoji(self):
        s = smart_truncate("👩🏽‍💻🚀 coding time", 8)
        self.assertTrue(s.endswith("…"))


class TestLevenshtein(unittest.TestCase):
    def test_basic(self):
        self.assertEqual(levenshtein("kitten", "sitting"), 3)
        self.assertEqual(levenshtein("", "abc"), 3)

    def test_emoji(self):
        # Family emoji are multiple code points; still fine in Python.
        a = "👩‍👩‍👧‍👧"
        b = "👨‍👩‍👧‍👦"
        d = levenshtein(a, b)
        self.assertIsInstance(d, int)
        self.assertGreaterEqual(d, 1)

    def test_max_cost_early_stop(self):
        d = levenshtein("a" * 200, "b" * 200, max_cost=2)
        self.assertGreater(d, 2)


class TestSafeFilename(unittest.TestCase):
    def test_invalid_chars_and_whitespace(self):
        s = safe_filename('  report:<v1>?*.txt  ')
        self.assertTrue(s.endswith(".txt"))
        self.assertNotIn(":", s)
        self.assertNotIn("?", s)
        self.assertFalse(s.startswith("."))
        self.assertFalse(s.endswith(" "))

    def test_reserved_names(self):
        self.assertEqual(safe_filename("CON"), "CON_")
        self.assertEqual(safe_filename("nul.TXT"), "nul.TXT_")  # base 'nul' -> nul.TXT_

    def test_hidden_and_length(self):
        s = safe_filename(".env", allow_hidden=False)
        self.assertNotEqual(s, ".env")
        s2 = safe_filename("x" * 300)
        self.assertLessEqual(len(s2), 255)
        self.assertNotEqual(s2, "")


if __name__ == "__main__":  # pragma: no cover
    unittest.main()
