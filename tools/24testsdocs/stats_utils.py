"""Stats utilities
==================

Lightweight numeric helpers that handle NaN/±∞ thoughtfully.

Functions
---------
- :func:`mean` – Arithmetic mean with configurable NaN policy.
- :func:`variance` – Sample/population variance; NaN/∞ rules documented.
- :func:`zscore` – Standard scores; constant vectors -> zeros.
- :func:`moving_average` – Uniform window, ``mode='valid'`` or ``'same'``.
- :func:`percentile` – 0–100 quantiles with linear interpolation.
- :func:`safe_div` – Division that handles zero/NaN/∞ predictably.
- :func:`is_monotonic` – Tests monotonicity (increasing/decreasing).

NaN policy
----------
``nan_policy`` is one of:

* ``'propagate'`` – if any NaN present, return NaN.
* ``'omit'`` – drop NaNs.
* ``'raise'`` – raise ``ValueError`` on NaN.

Infinity rules (summary)
------------------------
* Mean of ``[∞, finite...]`` -> ``∞`` (sign respected).
* Mean of ``[∞, -∞, ...]`` -> ``NaN`` (undefined).
* Variance with both ``∞`` and ``-∞`` -> ``NaN``.
* Variance with any ``∞`` (but not both signs) -> ``∞``.
"""

from __future__ import annotations

import math
from typing import Iterable, List, Sequence, Tuple


def _split_values(xs: Iterable[float]) -> Tuple[List[float], bool, bool, bool]:
    """Return (finite_list, has_nan, has_pos_inf, has_neg_inf)."""
    finite: List[float] = []
    has_nan = has_pos_inf = has_neg_inf = False
    for x in xs:
        if math.isnan(x):
            has_nan = True
        elif math.isinf(x):
            if x > 0:
                has_pos_inf = True
            else:
                has_neg_inf = True
        else:
            finite.append(float(x))
    return finite, has_nan, has_pos_inf, has_neg_inf


def _check_nan_policy(has_nan: bool, nan_policy: str) -> None:
    if nan_policy not in {"propagate", "omit", "raise"}:
        raise ValueError("nan_policy must be 'propagate', 'omit', or 'raise'")
    if has_nan:
        if nan_policy == "raise":
            raise ValueError("NaN encountered")
        if nan_policy == "propagate":
            raise ValueError("__PROPAGATE__")  # sentinel handled by callers


def mean(xs: Iterable[float], *, nan_policy: str = "omit") -> float:
    """Arithmetic mean.

    Returns NaN if there are no values after applying ``nan_policy``.
    """
    finite, has_nan, has_pos_inf, has_neg_inf = _split_values(xs)
    try:
        _check_nan_policy(has_nan, nan_policy)
    except ValueError as e:
        if str(e) == "__PROPAGATE__":
            return math.nan
        raise

    if has_pos_inf and has_neg_inf:
        return math.nan
    if has_pos_inf:
        return math.inf
    if has_neg_inf:
        return -math.inf

    if not finite:
        return math.nan
    return sum(finite) / float(len(finite))


def variance(
    xs: Iterable[float],
    *,
    ddof: int = 1,
    nan_policy: str = "omit",
) -> float:
    """Variance (sample by default).

    Returns
    -------
    float
        NaN if not enough values (``n - ddof <= 0``).
    """
    finite, has_nan, has_pos_inf, has_neg_inf = _split_values(xs)
    try:
        _check_nan_policy(has_nan, nan_policy)
    except ValueError as e:
        if str(e) == "__PROPAGATE__":
            return math.nan
        raise

    if has_pos_inf and has_neg_inf:
        return math.nan
    if has_pos_inf or has_neg_inf:
        # All finite vs any inf -> variability is unbounded.
        return math.inf

    n = len(finite)
    if n - ddof <= 0:
        return math.nan
    mu = sum(finite) / float(n)
    # Two-pass (more stable).
    sse = sum((x - mu) ** 2 for x in finite)
    return sse / float(n - ddof)


def zscore(xs: Sequence[float], *, nan_policy: str = "omit") -> List[float]:
    """Standard scores.

    Constant vectors map to all zeros (common practical choice).
    """
    finite, has_nan, has_pos_inf, has_neg_inf = _split_values(xs)
    try:
        _check_nan_policy(has_nan, nan_policy)
    except ValueError as e:
        if str(e) == "__PROPAGATE__":
            return [math.nan] * len(xs)
        raise

    if has_pos_inf and has_neg_inf:
        return [math.nan] * len(xs)
    if has_pos_inf or has_neg_inf:
        # Normalize with infinities present is undefined in practice.
        return [math.nan] * len(xs)

    if not finite:
        return [math.nan] * len(xs)

    mu = mean(finite, nan_policy="omit")
    var = variance(finite, ddof=0, nan_policy="omit")
    if var == 0 or math.isnan(var):
        # Return zeros for constant vectors; NaN if var is NaN.
        return [0.0 if var == 0 else math.nan for _ in xs]

    sigma = math.sqrt(var)
    out: List[float] = []
    for x in xs:
        if math.isnan(x) and nan_policy == "omit":
            out.append(math.nan)
        elif math.isinf(x):
            out.append(math.nan)
        else:
            out.append((float(x) - mu) / sigma)
    return out


def moving_average(
    xs: Sequence[float],
    window: int,
    *,
    mode: str = "valid",
) -> List[float]:
    """Uniform moving average.

    Parameters
    ----------
    xs:
        Sequence of numbers (NaN allowed; treated as missing).
    window:
        Window size (positive integer).
    mode:
        ``'valid'`` – only full windows (length ``n - w + 1``).
        ``'same'`` – centered output, same length, edges are NaN.

    Returns
    -------
    list[float]
    """
    if window <= 0:
        raise ValueError("window must be positive")
    if mode not in {"valid", "same"}:
        raise ValueError("mode must be 'valid' or 'same'")

    n = len(xs)
    if n == 0:
        return []

    # Precompute cumulative sums (skip NaNs by counting valid terms).
    sums = [0.0]
    counts = [0]
    for x in xs:
        if math.isnan(x):
            sums.append(sums[-1])
            counts.append(counts[-1])
        else:
            sums.append(sums[-1] + float(x))
            counts.append(counts[-1] + 1)

    def avg(lo: int, hi: int) -> float:
        s = sums[hi] - sums[lo]
        c = counts[hi] - counts[lo]
        return s / c if c > 0 else math.nan

    out: List[float] = []
    if mode == "valid":
        if n < window:
            return []
        for i in range(0, n - window + 1):
            out.append(avg(i, i + window))
        return out

    # mode == "same": centered, pad edges with NaN.
    half = window // 2
    for i in range(n):
        lo = i - half
        hi = lo + window
        if lo < 0 or hi > n:
            out.append(math.nan)
        else:
            out.append(avg(lo, hi))
    return out


def percentile(
    xs: Sequence[float],
    q: float,
    *,
    method: str = "linear",
    nan_policy: str = "omit",
) -> float:
    """Percentile in [0, 100] with linear interpolation.

    Raises
    ------
    ValueError
        On invalid ``q`` or when all values are NaN.
    """
    if not (0.0 <= q <= 100.0):
        raise ValueError("q must be in [0, 100]")

    finite, has_nan, _, _ = _split_values(xs)
    try:
        _check_nan_policy(has_nan, nan_policy)
    except ValueError as e:
        if str(e) == "__PROPAGATE__":
            return math.nan
        raise

    if not finite:
        raise ValueError("no valid values")

    x = sorted(finite)
    if q == 0:
        return x[0]
    if q == 100:
        return x[-1]

    pos = (len(x) - 1) * (q / 100.0)
    lo = int(math.floor(pos))
    hi = int(math.ceil(pos))
    h = pos - lo
    if method != "linear":
        raise ValueError("only method='linear' is supported")
    return x[lo] * (1.0 - h) + x[hi] * h


def safe_div(
    a: float,
    b: float,
    *,
    on_zero: str = "inf",
) -> float:
    """Divide with explicit zero handling.

    Parameters
    ----------
    a, b:
        Numerator and denominator.
    on_zero:
        Behavior when ``b == 0``:
        * ``'inf'`` (default): return signed infinity.
        * ``'zero'``: return 0.0 (signed 0 for -0 is not preserved).
        * ``'nan'``: return NaN.
        * ``'raise'``: raise ``ZeroDivisionError``.
    """
    if math.isnan(a) or math.isnan(b):
        return math.nan
    if b == 0:
        if on_zero == "inf":
            return math.copysign(math.inf, a if a != 0 else 1.0)
        if on_zero == "zero":
            return 0.0
        if on_zero == "nan":
            return math.nan
        if on_zero == "raise":
            raise ZeroDivisionError("division by zero")
        raise ValueError("on_zero must be 'inf', 'zero', 'nan', or 'raise'")
    return a / b


def is_monotonic(
    xs: Sequence[float],
    *,
    strict: bool = False,
    decreasing: bool = False,
) -> bool:
    """Check monotonicity (ignoring NaNs).

    Returns
    -------
    bool
        True if the finite subsequence is monotonic.
    """
    prev = None
    for x in xs:
        if math.isnan(x):
            continue
        xf = float(x)
        if prev is None:
            prev = xf
            continue
        if decreasing:
            if strict and not (xf < prev):
                return False
            if not strict and not (xf <= prev):
                return False
        else:
            if strict and not (xf > prev):
                return False
            if not strict and not (xf >= prev):
                return False
        prev = xf
    return True
