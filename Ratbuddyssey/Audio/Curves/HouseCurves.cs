using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Audyssey.MultEQApp;

namespace Ratbuddyssey
{
    /// <summary>
    /// A small, opinionated catalogue of room-correction "house curves" that can
    /// be applied to a channel's <see cref="DetectedChannel.CustomTargetCurvePointsDictionary"/>.
    ///
    /// All curves are defined in the home / studio domain — IEM and car-tuning
    /// targets are intentionally omitted because Audyssey runs only on
    /// loudspeakers in real rooms, where those curves would be misleading.
    /// </summary>
    public static class HouseCurves
    {
        /// <summary>One (frequency Hz, gain dB) breakpoint on the target curve.</summary>
        public readonly record struct Point(double FrequencyHz, double GainDb);

        public sealed record HouseCurve(string Name, string Description, IReadOnlyList<Point> Points);

        // Audyssey's editor accepts 10 Hz - 24 kHz and roughly -20..+12 dB; the
        // values below stay comfortably inside that envelope. Frequencies below
        // 20 Hz are not used because every shipping receiver high-passes there
        // anyway, so a target point would just be cosmetic.

        private static readonly HouseCurve _flat = new(
            "Flat (0 dB reference)",
            "No tonal tilt; useful as a baseline before adding your own taste.",
            new[]
            {
                new Point(20, 0),
                new Point(20000, 0),
            });

        // Harman / Olive target for loudspeakers in rooms: gentle bass shelf
        // (~+5 dB at 30 Hz tapering to flat by ~120 Hz) and a smooth treble
        // roll-off (~-1 dB at 1 kHz, ~-6 dB at 10 kHz, ~-8 dB at 20 kHz).
        // Source: Olive, "A New Reference Listening Room for Consumer, Professional
        // and Automotive Audio Research", AES 2014.
        private static readonly HouseCurve _harman = new(
            "Harman (loudspeaker / room)",
            "Olive/Harman in-room target: gentle bass shelf and smooth treble tilt.",
            new[]
            {
                new Point(20,    6),
                new Point(40,    5),
                new Point(80,    3),
                new Point(120,   0),
                new Point(1000,  0),
                new Point(3000, -1),
                new Point(6000, -3),
                new Point(10000,-6),
                new Point(16000,-7),
                new Point(20000,-8),
            });

        // Bruel &amp; Kjaer 1974 reference: flat to ~200 Hz, then -1 dB/octave
        // through the top of the audio band. 200 Hz to 20 kHz is ~6.6 octaves.
        private static readonly HouseCurve _bk1974 = new(
            "B&K 1974 (-1 dB/oct)",
            "Classic gentle downward tilt of about 1 dB per octave above 200 Hz.",
            new[]
            {
                new Point(20,     0),
                new Point(200,    0),
                new Point(400,   -1),
                new Point(800,   -2),
                new Point(1600,  -3),
                new Point(3200,  -4),
                new Point(6400,  -5),
                new Point(12800, -6),
                new Point(20000, -7),
            });

        // JBL "Synthesis"-style curve: similar shape to Harman but with a
        // slightly more pronounced bass shelf and a flatter midrange.
        private static readonly HouseCurve _jbl = new(
            "JBL (cinema / synthesis)",
            "Flat-ish midrange with a noticeable bass lift and treble roll-off.",
            new[]
            {
                new Point(20,    5),
                new Point(60,    4),
                new Point(100,   2),
                new Point(160,   0),
                new Point(3000,  0),
                new Point(6000, -2),
                new Point(10000,-4),
                new Point(16000,-5),
                new Point(20000,-6),
            });

        public static IReadOnlyList<HouseCurve> All { get; } = new[]
        {
            _flat, _harman, _bk1974, _jbl,
        };

        /// <summary>
        /// Returns the unmodified base preset points for <paramref name="curve"/>.
        /// Existing presets are returned by reference and must not be mutated.
        /// </summary>
        public static IReadOnlyList<Point> GetPreset(HouseCurve curve)
        {
            if (curve == null) return Array.Empty<Point>();
            return curve.Points;
        }

        /// <summary>
        /// Returns a new list of points = preset shaped by <paramref name="settings"/>
        /// (bass shelf, treble tilt, strength blend). The preset is not mutated.
        /// </summary>
        public static IReadOnlyList<Point> GetPresetWithSettings(
            HouseCurve curve,
            Audio.Curves.CurveSettings settings)
        {
            if (curve == null) return Array.Empty<Point>();
            return Audio.Curves.CurveModifier.ApplyCurveSettings(curve.Points, settings);
        }

        /// <summary>
        /// Subwoofer channels are low-passed by Audyssey at or below ~120 Hz,
        /// so any target points above this frequency are meaningless on them.
        /// We trim sub curves at 200 Hz (a comfortable margin above the highest
        /// realistic crossover) and append an interpolated anchor at the cut so
        /// the bass-shelf shape is preserved without a discontinuity.
        /// </summary>
        private const double SubwooferCurveCutoffHz = 200.0;

        /// <summary>
        /// Replaces the channel's existing custom target points with the
        /// supplied curve. For full-range channels the curve is applied as-is.
        /// For subwoofer channels (CommandId SW*) the curve is trimmed to
        /// <see cref="SubwooferCurveCutoffHz"/> with an interpolated anchor at
        /// the cut frequency — a sub doesn't reproduce the treble roll-off,
        /// but it absolutely should track the bass-shelf portion of the curve.
        /// Coordinates are emitted in invariant culture so they round-trip
        /// through Audyssey's JSON unchanged.
        /// </summary>
        public static void ApplyTo(DetectedChannel channel, HouseCurve curve)
        {
            if (channel == null || curve == null) return;

            var sorted = curve.Points.OrderBy(p => p.FrequencyHz).ToList();
            IEnumerable<Point> toWrite = sorted;

            if (Audyssey.AudysseyHardwareQuirks.IsSubwoofer(channel))
            {
                var trimmed = new List<Point>();
                foreach (var p in sorted)
                {
                    if (p.FrequencyHz <= SubwooferCurveCutoffHz) trimmed.Add(p);
                }
                // Append an anchor at the cutoff so the curve has a clean endpoint
                // even when the original curve's next point is well above 200 Hz
                // (e.g. Harman jumps from 120 Hz to 1 kHz).
                if (sorted.Count > 0 && (trimmed.Count == 0 || trimmed[^1].FrequencyHz < SubwooferCurveCutoffHz))
                {
                    double anchorDb = InterpolateDb(sorted, SubwooferCurveCutoffHz);
                    trimmed.Add(new Point(SubwooferCurveCutoffHz, anchorDb));
                }
                toWrite = trimmed;
            }

            channel.CustomTargetCurvePointsDictionary.Clear();
            foreach (var p in toWrite)
            {
                channel.CustomTargetCurvePointsDictionary.Add(new MyKeyValuePair(
                    p.FrequencyHz.ToString("0.###", CultureInfo.InvariantCulture),
                    p.GainDb.ToString("0.###", CultureInfo.InvariantCulture)));
            }
        }

        /// <summary>Linear-in-frequency dB interpolation. Clamps outside the curve's range.</summary>
        private static double InterpolateDb(List<Point> sortedPoints, double hz)
        {
            if (sortedPoints.Count == 0) return 0.0;
            if (hz <= sortedPoints[0].FrequencyHz) return sortedPoints[0].GainDb;
            if (hz >= sortedPoints[^1].FrequencyHz) return sortedPoints[^1].GainDb;
            for (int i = 1; i < sortedPoints.Count; i++)
            {
                var a = sortedPoints[i - 1];
                var b = sortedPoints[i];
                if (hz <= b.FrequencyHz)
                {
                    double t = (hz - a.FrequencyHz) / (b.FrequencyHz - a.FrequencyHz);
                    return a.GainDb + t * (b.GainDb - a.GainDb);
                }
            }
            return sortedPoints[^1].GainDb;
        }

        /// <summary>
        /// Applies <paramref name="curve"/> to every applicable channel in
        /// <paramref name="channels"/> and returns the number of channels
        /// touched. Subwoofers receive a trimmed bass-only version of the
        /// curve (see <see cref="ApplyTo"/>) so they get the bass-shelf shape
        /// without the meaningless treble points.
        /// </summary>
        public static int ApplyToAll(IEnumerable<DetectedChannel> channels, HouseCurve curve)
        {
            if (channels == null || curve == null) return 0;
            int n = 0;
            foreach (var ch in channels)
            {
                // EnChannelType 55 is "subwoofer-only no full-range target" —
                // matches the existing ShouldSerializeCustomTargetCurvePoints
                // suppression so we don't write points the receiver will ignore.
                if (ch.EnChannelType == 55) continue;
                ApplyTo(ch, curve);
                n++;
            }
            return n;
        }
    }
}
