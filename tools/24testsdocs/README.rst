Two-Module Demo with Comprehensive Tests
=======================================

Overview
--------
This repository contains two standalone Python modules:

* ``text_utils.py`` – Unicode-first text helpers (normalize, slugify, etc.).
* ``stats_utils.py`` – Small numeric toolkit with careful NaN/∞ handling.

Both modules include reStructuredText (PEP 287) docstrings and follow PEP 8.

Running Tests
-------------
Use the standard library:

.. code-block:: bash

   python -m unittest discover -s tests -p "test_*.py" -v

Test Coverage Philosophy
------------------------
The test suite goes beyond "happy paths" and checks:

* **Unicode:** Chinese/Arabic, emoji, combining marks, case folding.
* **Pathological sizes:** very long strings (10k characters).
* **Non-finite numbers:** ``NaN``, ``+∞``, ``-∞`` and their interactions.
* **Edge semantics:** constant vectors for z-scores, window edges in
  moving averages, Windows-reserved filenames, and early-stop edit distance.

File List
---------
- ``text_utils.py``
- ``stats_utils.py``
- ``tests/test_text_utils.py``
- ``tests/test_stats_utils.py``

Notes
-----
* Slugify default (``transliterate=True``) attempts ASCII; non-Latin scripts
  may drop out. Use ``transliterate=False`` to keep scripts like Arabic/Chinese.
* ``zscore`` returns zeros for constant vectors, which is common in practice
  to avoid NaN propagation in downstream pipelines.
