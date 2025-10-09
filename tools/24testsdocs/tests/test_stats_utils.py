import math
import unittest

from stats_utils import (
    is_monotonic,
    mean,
    moving_average,
    percentile,
    safe_div,
    variance,
    zscore,
)


class TestMeanVariance(unittest.TestCase):
    def test_mean_basic_and_nan_policies(self):
        self.assertAlmostEqual(mean([1, 2, 3]), 2.0)
        self.assertTrue(math.isnan(mean([], nan_policy="omit")))
        with self.assertRaises(ValueError):
            mean([1, math.nan], nan_policy="raise")
        self.assertTrue(math.isnan(mean([1, math.nan], nan_policy="propagate")))
        self.assertAlmostEqual(mean([1, math.nan], nan_policy="omit"), 1.0)

    def test_mean_infinities(self):
        self.assertEqual(mean([1.0, math.inf]), math.inf)
        self.assertEqual(mean([-1.0, -math.inf]), -math.inf)
        self.assertTrue(math.isnan(mean([math.inf, -math.inf, 1.0])))

    def test_variance_sample_population(self):
        xs = [1, 2, 3, 4]
        self.assertAlmostEqual(variance(xs, ddof=0), 1.25)
        self.assertAlmostEqual(variance(xs, ddof=1), 1.6666666667, places=6)
        self.assertTrue(math.isnan(variance([5], ddof=1)))

    def test_variance_infinities(self):
        self.assertTrue(math.isinf(variance([1, 2, math.inf])))
        self.assertTrue(math.isnan(variance([math.inf, -math.inf, 0.0])))

    def test_zscore(self):
        self.assertEqual(zscore([5, 5, 5]), [0.0, 0.0, 0.0])
        zs = zscore([1, 2, 3])
        self.assertAlmostEqual(sum(zs), 0.0, places=7)
        self.assertTrue(all(math.isfinite(z) for z in zs))
        # With NaNs or infinities -> NaNs in output
        self.assertTrue(all(math.isnan(z) for z in zscore([1, math.inf, 2])))

    def test_nan_policy_propagate(self):
        zs = zscore([1, math.nan, 2], nan_policy="propagate")
        self.assertTrue(all(math.isnan(z) for z in zs))


class TestMovingAverage(unittest.TestCase):
    def test_valid_mode(self):
        xs = [1, 2, 3, 4]
        self.assertEqual(moving_average(xs, 2, mode="valid"), [1.5, 2.5, 3.5])
        self.assertEqual(moving_average(xs, 5, mode="valid"), [])

    def test_same_mode_and_nans(self):
        xs = [1.0, math.nan, 3.0, 5.0]
        out = moving_average(xs, 3, mode="same")
        self.assertEqual(len(out), len(xs))
        # centered: edges are NaN
        self.assertTrue(math.isnan(out[0]))
        self.assertTrue(math.isnan(out[-1]))
        # middle window ignores NaN
        self.assertAlmostEqual(out[2], (3.0 + 5.0) / 2.0)

    def test_invalid_params(self):
        with self.assertRaises(ValueError):
            moving_average([1, 2], 0)
        with self.assertRaises(ValueError):
            moving_average([1, 2], 2, mode="foo")


class TestPercentile(unittest.TestCase):
    def test_basic_percentiles(self):
        xs = [1, 2, 3, 4]
        self.assertEqual(percentile(xs, 0), 1)
        self.assertEqual(percentile(xs, 100), 4)
        self.assertAlmostEqual(percentile(xs, 50), 2.5)

    def test_linear_interpolation(self):
        xs = [10, 20, 30]
        self.assertAlmostEqual(percentile(xs, 25), 15.0)
        self.assertAlmostEqual(percentile(xs, 75), 25.0)

    def test_nan_policy_and_errors(self):
        with self.assertRaises(ValueError):
            percentile([], 50)
        with self.assertRaises(ValueError):
            percentile([1, 2, 3], -1)
        with self.assertRaises(ValueError):
            percentile([1, math.nan], 50, nan_policy="raise")
        self.assertTrue(math.isnan(percentile([1, math.nan], 50, nan_policy="propagate")))


class TestSafeDivAndMonotonic(unittest.TestCase):
    def test_safe_div(self):
        self.assertEqual(safe_div(1.0, 0.0), math.inf)
        self.assertEqual(safe_div(-1.0, 0.0), -math.inf)
        self.assertTrue(math.isnan(safe_div(0.0, 0.0, on_zero="nan")))
        self.assertEqual(safe_div(10.0, 0.0, on_zero="zero"), 0.0)
        with self.assertRaises(ZeroDivisionError):
            safe_div(1.0, 0.0, on_zero="raise")

    def test_is_monotonic(self):
        self.assertTrue(is_monotonic([1, 2, 2, 3]))
        self.assertFalse(is_monotonic([3, 2, 3]))
        self.assertTrue(is_monotonic([3, 2, 1], decreasing=True))
        self.assertFalse(is_monotonic([3, 3, 2], decreasing=True, strict=True))
        self.assertTrue(is_monotonic([math.nan, 1, 2, math.nan, 2], strict=False))


if __name__ == "__main__":  # pragma: no cover
    unittest.main()
