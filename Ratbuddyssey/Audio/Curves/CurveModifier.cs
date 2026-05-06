using System;
using System.Collections.Generic;
using Ratbuddyssey;

namespace Ratbuddyssey.Audio.Curves
{
    /// <summary>
    /// Applies <see cref="CurveSettings"/> to a base curve non-destructively.
    /// Reuses <see cref="HouseCurves.Point"/> as the curve-point model rather
    /// than introducing a parallel type.
    /// </summary>
    public static class CurveModifier
    {
        // Bass shelf taper: full effect at/below 20 Hz, zero at/above 200 Hz,
        // log-frequency linear in between. Deterministic and cheap.
        private const double BassFullHz = 20.0;
        private const double BassZeroHz = 200.0;

        // Treble tilt taper: zero at/below 2 kHz, full effect at/above 20 kHz.
        private const double TrebleZeroHz = 2000.0;
        private const double TrebleFullHz = 20000.0;

        /// <summary>
        /// Returns a new list of points with <paramref name="settings"/> applied
        /// at the same frequencies as <paramref name="baseCurve"/>. The input
        /// list and its points are not mutated.
        /// </summary>
        public static IReadOnlyList<HouseCurves.Point> ApplyCurveSettings(
            IReadOnlyList<HouseCurves.Point> baseCurve,
            CurveSettings settings)
        {
            if (baseCurve == null) return Array.Empty<HouseCurves.Point>();
            if (settings == null) settings = new CurveSettings();

            var result = new List<HouseCurves.Point>(baseCurve.Count);
            for (int i = 0; i < baseCurve.Count; i++)
            {
                var p = baseCurve[i];
                double gain = p.GainDb * settings.Strength
                            + BassShelf(p.FrequencyHz, settings.BassBoostDb)
                            + TrebleTilt(p.FrequencyHz, settings.TrebleTiltDb);
                result.Add(new HouseCurves.Point(p.FrequencyHz, gain));
            }
            return result;
        }

        private static double BassShelf(double hz, double boostDb)
        {
            if (boostDb == 0.0) return 0.0;
            if (hz <= BassFullHz) return boostDb;
            if (hz >= BassZeroHz) return 0.0;
            // Linear in log-frequency: 1 at BassFullHz, 0 at BassZeroHz.
            double t = (Math.Log10(BassZeroHz) - Math.Log10(hz))
                     / (Math.Log10(BassZeroHz) - Math.Log10(BassFullHz));
            return boostDb * t;
        }

        private static double TrebleTilt(double hz, double tiltDb)
        {
            if (tiltDb == 0.0) return 0.0;
            if (hz <= TrebleZeroHz) return 0.0;
            if (hz >= TrebleFullHz) return tiltDb;
            // Linear in log-frequency: 0 at TrebleZeroHz, 1 at TrebleFullHz.
            double t = (Math.Log10(hz) - Math.Log10(TrebleZeroHz))
                     / (Math.Log10(TrebleFullHz) - Math.Log10(TrebleZeroHz));
            return tiltDb * t;
        }
    }
}
