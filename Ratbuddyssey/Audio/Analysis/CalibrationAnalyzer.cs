using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Audyssey;
using Audyssey.MultEQApp;
using MathNet.Numerics.IntegralTransforms;

// IList<DetectedChannel> is the right abstraction for these private helpers; the
// caller passes ObservableCollection<DetectedChannel>, but constraining the
// helpers to the interface keeps them testable. CA1859's perf hint is noise here.
#pragma warning disable CA1859

namespace Ratbuddyssey.Audio.Analysis;

/// <summary>
/// First-pass calibration warning engine. Runs a small suite of conservative
/// rules over an <see cref="AudysseyMultEQApp"/> (the project's calibration
/// model) and returns structured <see cref="CalibrationWarning"/>s.
///
/// <para>
/// Design notes:
/// <list type="bullet">
///   <item>Each rule is a private <c>Analyze*</c> method appending to a shared
///         list — easy to add/remove/reorder rules without touching the
///         entry point.</item>
///   <item>Every rule must tolerate missing/garbage data (null collections,
///         non-numeric trim strings, etc.). Audyssey .ady files in the wild
///         are uneven; we'd rather skip a rule than crash the analyzer.</item>
///   <item>Severity levels follow <see cref="CalibrationWarningSeverity"/>:
///         <c>Info</c> for FYI, <c>Warning</c> for "user should review",
///         <c>Critical</c> only for hard-limit violations.</item>
/// </list>
/// </para>
/// </summary>
public sealed class CalibrationAnalyzer
{
    /// <summary>Trim values whose absolute magnitude exceeds this trigger a warning.</summary>
    private const decimal TrimSoftLimitDb = 10m;

    /// <summary>How far apart the front-pair trims may diverge before we flag imbalance (dB).</summary>
    private const decimal FrontPairImbalanceDb = 3m;

    /// <summary>Rule-3 valid crossover list per spec — note: a stricter subset of
    /// <see cref="AudysseyHardwareQuirks.ValidCrossoverFrequenciesHz"/>.</summary>
    private static readonly int[] ValidCrossoverHz = { 40, 60, 80, 90, 100, 110, 120, 150 };

    /// <summary>Mains crossover below this is "unusually low" given typical sub integration (Hz).</summary>
    private const int UnusuallyLowMainsCrossoverHz = 60;

    /// <summary>Sub trim is "near limits" when within this many dB of the receiver's clip edge.</summary>
    private const decimal SubTrimNearLimitMarginDb = 2m;

    /// <summary>Run every rule and return the combined finding list (never null).</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Public analyzer API surface; instance method per spec.")]
    public IReadOnlyList<CalibrationWarning> Analyze(AudysseyMultEQApp calibration)
    {
        var warnings = new List<CalibrationWarning>();
        if (calibration == null) return warnings;

        var channels = calibration.DetectedChannels;
        if (channels == null || channels.Count == 0) return warnings;

        AnalyzeTrimLimits(channels, warnings);
        AnalyzeFrontPairBalance(channels, warnings);
        AnalyzeCrossovers(channels, warnings);
        AnalyzeSubwooferIntegration(channels, warnings);
        AnalyzeLowFrequencyNulls(channels, warnings);

        return warnings;
    }

    // ---------------------------------------------------------------------
    // Rule 1: trim limit
    // ---------------------------------------------------------------------
    private static void AnalyzeTrimLimits(
        IList<DetectedChannel> channels,
        List<CalibrationWarning> warnings)
    {
        for (int i = 0; i < channels.Count; i++)
        {
            var ch = channels[i];
            if (ch == null) continue;
            if (!TryParseDecimal(ch.TrimAdjustment, out decimal trim)) continue;

            if (trim > TrimSoftLimitDb)
            {
                warnings.Add(new CalibrationWarning(
                    Code: "trim.aboveLimit",
                    Severity: CalibrationWarningSeverity.Warning,
                    Channel: ch.CommandId,
                    Message: Fmt("Trim {0:+0.##;-0.##;0} dB exceeds the +{1:0.##} dB soft limit.", trim, TrimSoftLimitDb)));
            }
            else if (trim < -TrimSoftLimitDb)
            {
                warnings.Add(new CalibrationWarning(
                    Code: "trim.belowLimit",
                    Severity: CalibrationWarningSeverity.Warning,
                    Channel: ch.CommandId,
                    Message: Fmt("Trim {0:+0.##;-0.##;0} dB is below the -{1:0.##} dB soft limit.", trim, TrimSoftLimitDb)));
            }
        }
    }

    // ---------------------------------------------------------------------
    // Rule 2: front L/R imbalance
    // ---------------------------------------------------------------------
    private static void AnalyzeFrontPairBalance(
        IList<DetectedChannel> channels,
        List<CalibrationWarning> warnings)
    {
        DetectedChannel fl = FindByCommandId(channels, "FL");
        DetectedChannel fr = FindByCommandId(channels, "FR");
        if (fl == null || fr == null) return;

        if (!TryParseDecimal(fl.TrimAdjustment, out decimal flTrim)) return;
        if (!TryParseDecimal(fr.TrimAdjustment, out decimal frTrim)) return;

        decimal diff = Math.Abs(flTrim - frTrim);
        if (diff > FrontPairImbalanceDb)
        {
            warnings.Add(new CalibrationWarning(
                Code: "frontPair.trimImbalance",
                Severity: CalibrationWarningSeverity.Warning,
                Channel: "FL/FR",
                Message: Fmt("Front L/R trim differs by {0:0.##} dB (FL {1:+0.##;-0.##;0}, FR {2:+0.##;-0.##;0}); expected within {3:0.##} dB.",
                    diff, flTrim, frTrim, FrontPairImbalanceDb)));
        }
    }

    // ---------------------------------------------------------------------
    // Rule 3: crossover validation against canonical list
    // ---------------------------------------------------------------------
    private static void AnalyzeCrossovers(
        IList<DetectedChannel> channels,
        List<CalibrationWarning> warnings)
    {
        for (int i = 0; i < channels.Count; i++)
        {
            var ch = channels[i];
            if (ch == null) continue;
            if (AudysseyHardwareQuirks.IsSubwoofer(ch)) continue;
            if (string.IsNullOrEmpty(ch.CustomCrossover)) continue;
            // "F" = full-range / large; not a crossover frequency.
            if (string.Equals(ch.CustomCrossover, "F", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(ch.CustomCrossover, NumberStyles.Integer, CultureInfo.InvariantCulture, out int xo)) continue;

            bool valid = false;
            for (int k = 0; k < ValidCrossoverHz.Length; k++)
            {
                if (ValidCrossoverHz[k] == xo) { valid = true; break; }
            }
            if (!valid)
            {
                warnings.Add(new CalibrationWarning(
                    Code: "crossover.nonStandard",
                    Severity: CalibrationWarningSeverity.Warning,
                    Channel: ch.CommandId,
                    Message: Fmt("Crossover {0} Hz is not one of the standard values (40, 60, 80, 90, 100, 110, 120, 150).", xo)));
            }
        }
    }

    // ---------------------------------------------------------------------
    // Rule 4: subwoofer integration
    // ---------------------------------------------------------------------
    private static void AnalyzeSubwooferIntegration(
        IList<DetectedChannel> channels,
        List<CalibrationWarning> warnings)
    {
        // 4a. Sub trim near hardware limits.
        decimal subTrimFloor = AudysseyHardwareQuirks.GetSubwooferTrimFloorDb(channels);
        decimal hardCeiling = AudysseyHardwareQuirks.MaxAbsoluteTrimDb;

        for (int i = 0; i < channels.Count; i++)
        {
            var ch = channels[i];
            if (ch == null || !AudysseyHardwareQuirks.IsSubwoofer(ch)) continue;

            if (TryParseDecimal(ch.TrimAdjustment, out decimal trim))
            {
                if (trim >= hardCeiling - SubTrimNearLimitMarginDb)
                {
                    warnings.Add(new CalibrationWarning(
                        Code: "sub.trimNearCeiling",
                        Severity: CalibrationWarningSeverity.Warning,
                        Channel: ch.CommandId,
                        Message: Fmt("Subwoofer trim {0:+0.##;-0.##;0} dB is within {1:0.##} dB of the +{2:0.##} dB hardware ceiling — turn the sub up at the amp and re-run Audyssey for headroom.",
                            trim, SubTrimNearLimitMarginDb, hardCeiling)));
                }
                else if (trim <= subTrimFloor + SubTrimNearLimitMarginDb)
                {
                    warnings.Add(new CalibrationWarning(
                        Code: "sub.trimNearFloor",
                        Severity: CalibrationWarningSeverity.Warning,
                        Channel: ch.CommandId,
                        Message: Fmt("Subwoofer trim {0:+0.##;-0.##;0} dB is within {1:0.##} dB of the {2:+0.##;-0.##;0} dB hardware floor — turn the sub down at the amp and re-run Audyssey for headroom.",
                            trim, SubTrimNearLimitMarginDb, subTrimFloor)));
                }
            }
        }

        // 4b. Mains crossover unusually low (< 60 Hz on a small/sat speaker is
        //     usually a setup mistake) or wildly mismatched across the front
        //     stage when subs exist (typical Audyssey output is consistent for
        //     L/R/C; large divergence implies a tower vs satellite mismatch).
        bool hasSub = HasSubwoofer(channels);
        if (!hasSub) return;

        int? minMainsXo = null, maxMainsXo = null;
        for (int i = 0; i < channels.Count; i++)
        {
            var ch = channels[i];
            if (ch == null || AudysseyHardwareQuirks.IsSubwoofer(ch)) continue;
            if (!IsFrontStage(ch.CommandId)) continue;
            if (string.IsNullOrEmpty(ch.CustomCrossover)) continue;
            if (string.Equals(ch.CustomCrossover, "F", StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(ch.CustomCrossover, NumberStyles.Integer, CultureInfo.InvariantCulture, out int xo)) continue;

            if (xo < UnusuallyLowMainsCrossoverHz)
            {
                warnings.Add(new CalibrationWarning(
                    Code: "sub.mainsCrossoverLow",
                    Severity: CalibrationWarningSeverity.Info,
                    Channel: ch.CommandId,
                    Message: Fmt("Mains crossover {0} Hz is unusually low for a sub-integrated system; verify the speaker can actually reach below {1} Hz before trusting it.",
                        xo, UnusuallyLowMainsCrossoverHz)));
            }
            if (minMainsXo == null || xo < minMainsXo) minMainsXo = xo;
            if (maxMainsXo == null || xo > maxMainsXo) maxMainsXo = xo;
        }

        if (minMainsXo.HasValue && maxMainsXo.HasValue && (maxMainsXo.Value - minMainsXo.Value) >= 40)
        {
            warnings.Add(new CalibrationWarning(
                Code: "sub.mainsCrossoverMismatch",
                Severity: CalibrationWarningSeverity.Info,
                Channel: null,
                Message: Fmt("Front-stage crossovers span {0}..{1} Hz; large spreads can leave a hand-off gap with the sub. Review L/R/C consistency.",
                    minMainsXo.Value, maxMainsXo.Value)));
        }
    }

    // ---------------------------------------------------------------------
    // Rule 5: low-frequency null detection (averaged response, 20..200 Hz)
    // ---------------------------------------------------------------------
    /// <summary>
    /// Detect dips deeper than <c>10 dB</c> relative to a local mean in the
    /// <c>20..200 Hz</c> band of the per-channel averaged response.
    ///
    /// <para>
    /// The detector runs an FFT identical to the chart pipeline
    /// (<c>BuildSpectrumInput</c>) and then applies <c>1/6</c>-octave
    /// triangular smoothing in dB before measuring dips. Without smoothing,
    /// individual FFT bins can sit 15-30 dB below their neighbors thanks to
    /// comb filtering between mic positions — those vanish under any normal
    /// chart smoothing, so reporting them as "nulls" is misleading. The
    /// smoothed view matches what a user actually sees on the response chart.
    /// </para>
    ///
    /// <para>
    /// "Local mean" is a frequency-relative window (<c>±1/3</c> octave around
    /// the candidate, excluding the candidate itself), so the comparison
    /// scales correctly with frequency: at 30 Hz it spans ~24 to 38 Hz, at
    /// 150 Hz it spans ~119 to 189 Hz. This catches modal nulls (which sit
    /// in a sea of higher-energy bins) without being fooled by a broadband
    /// shelf or the crossover skirt.
    /// </para>
    /// </summary>
    private static void AnalyzeLowFrequencyNulls(
        IList<DetectedChannel> channels,
        List<CalibrationWarning> warnings)
    {
        const double defaultBandLowHz = 20;
        const double bandHighHz = 200;
        const double dipThresholdDb = 10;
        const double smoothingOctaves = 1.0 / 6.0; // matches chart's perceptual view
        const double localMeanOctaves = 1.0 / 3.0; // ±1/3 oct around the candidate bin
        const double sampleRate = 48000.0;

        for (int i = 0; i < channels.Count; i++)
        {
            var ch = channels[i];
            if (ch == null) continue;
            if (AudysseyHardwareQuirks.IsSubwoofer(ch)) continue; // subs are dominated by room modes by design

            // A "Small" speaker is high-passed by the receiver at its custom
            // crossover; anything below that is the sub's job and will look
            // like a dip in the speaker's own averaged response by definition.
            // Skip those bins so we don't false-positive on the crossover skirt
            // (e.g. "null at 17 Hz" on a tower crossed at 80 Hz).
            double bandLowHz = defaultBandLowHz;
            if (TryParseDecimal(ch.CustomCrossover, out decimal xo) && xo > 0)
            {
                // Start one half-octave above the -6 dB crossover point so the
                // analyzer is firmly inside the speaker's pass-band. 1.4× ≈ +0.5 oct.
                double xoHz = (double)xo * 1.4;
                if (xoHz > bandLowHz) bandLowHz = xoHz;
            }
            if (bandLowHz >= bandHighHz) continue; // nothing left to analyze

            double[] avg = ResponseAveraging.GetAveragedChannelResponse(ch);
            if (avg.Length < 64) continue; // FFT below this is meaningless

            // Round down to a power of two; MathNet's Fourier.Forward is happiest there.
            int n = HighestPowerOfTwo(avg.Length);
            if (n < 64) continue;

            var c = new Complex[n];
            for (int k = 0; k < n; k++) c[k] = avg[k];

            try { Fourier.Forward(c); }
            catch (Exception) { continue; } // never let a malformed channel break the run

            // Single-sided magnitude in dB (uncalibrated; only shape matters).
            int half = n / 2;
            var mag = new double[half];
            for (int k = 0; k < half; k++)
            {
                double m = c[k].Magnitude;
                mag[k] = m > 1e-12 ? 20.0 * Math.Log10(m) : -240.0;
            }

            // Apply 1/N-octave triangular smoothing in dB so the analyzer
            // operates on the same shape the user sees on the chart. Without
            // this step, a single comb-filter bin can sit 20+ dB below its
            // neighbors and trigger a phantom "null" warning even though the
            // smoothed response in that region is essentially flat.
            double[] smag = SmoothFractionalOctave(mag, sampleRate, n, smoothingOctaves);

            int loBin = (int)Math.Floor(bandLowHz * n / sampleRate);
            int hiBin = (int)Math.Ceiling(bandHighHz * n / sampleRate);
            if (loBin < 1) loBin = 1;
            if (hiBin >= half) hiBin = half - 1;
            if (hiBin <= loBin + 2) continue;

            double worstDip = 0;
            int worstBin = -1;
            double winFactor = Math.Pow(2.0, localMeanOctaves);
            for (int k = loBin; k <= hiBin; k++)
            {
                double freqK = k * sampleRate / n;
                int a = (int)Math.Floor(freqK / winFactor * n / sampleRate);
                int b = (int)Math.Ceiling(freqK * winFactor * n / sampleRate);
                if (a < loBin) a = loBin;
                if (b > hiBin) b = hiBin;
                if (b - a < 2) continue;

                double sum = 0; int cnt = 0;
                for (int t = a; t <= b; t++) { if (t == k) continue; sum += smag[t]; cnt++; }
                if (cnt == 0) continue;
                double localMean = sum / cnt;
                double dip = localMean - smag[k]; // positive = dip
                if (dip > worstDip) { worstDip = dip; worstBin = k; }
            }

            if (worstDip > dipThresholdDb && worstBin > 0)
            {
                double freq = worstBin * sampleRate / n;
                warnings.Add(new CalibrationWarning(
                    Code: "lowFreq.null",
                    Severity: CalibrationWarningSeverity.Info,
                    Channel: ch.CommandId,
                    Message: Fmt("Possible null near {0:0.#} Hz (~{1:0.#} dB below local mean) in averaged response. Consider mic-position or speaker placement.",
                        freq, worstDip)));
            }
        }
    }

    /// <summary>
    /// 1/<paramref name="fractionalOctaves"/>-octave smoothing of a dB
    /// magnitude vector, evaluated in the linear domain so deep nulls don't
    /// dominate. The window width grows with frequency: at bin <c>k</c>
    /// (frequency <c>f</c>) we average bins from <c>f / 2^(oct/2)</c> to
    /// <c>f · 2^(oct/2)</c>. This matches the well-known octave-smoothing
    /// behavior used in REW / ARTA and means a single razor-thin FFT null is
    /// blended with its (much higher) neighbors, removing comb-filter noise
    /// without distorting genuine broadband dips.
    /// </summary>
    private static double[] SmoothFractionalOctave(double[] mag, double sampleRate, int n, double fractionalOctaves)
    {
        int half = mag.Length;
        var output = new double[half];
        double halfWidth = Math.Pow(2.0, fractionalOctaves * 0.5);
        for (int k = 0; k < half; k++)
        {
            if (k == 0) { output[0] = mag[0]; continue; }
            double freq = k * sampleRate / n;
            int a = (int)Math.Floor(freq / halfWidth * n / sampleRate);
            int b = (int)Math.Ceiling(freq * halfWidth * n / sampleRate);
            if (a < 1) a = 1;
            if (b >= half) b = half - 1;
            if (b < a) { output[k] = mag[k]; continue; }
            // Triangular weighting centered on k gives a smoother result than
            // a hard rectangular window, especially at the edges of the band.
            double sumW = 0, sumWX = 0;
            int span = Math.Max(b - k, k - a);
            if (span <= 0) { output[k] = mag[k]; continue; }
            for (int t = a; t <= b; t++)
            {
                double w = 1.0 - Math.Abs(t - k) / (double)span;
                if (w <= 0) continue;
                // Average linear power, not dB, so a -60 dB bin contributes ~0
                // instead of dragging the mean down.
                double lin = Math.Pow(10.0, mag[t] / 20.0);
                sumWX += w * lin;
                sumW += w;
            }
            double meanLin = sumW > 0 ? sumWX / sumW : 0;
            output[k] = meanLin > 1e-12 ? 20.0 * Math.Log10(meanLin) : mag[k];
        }
        return output;
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------
    private static DetectedChannel FindByCommandId(IList<DetectedChannel> channels, string commandId)
    {
        for (int i = 0; i < channels.Count; i++)
        {
            var ch = channels[i];
            if (ch != null && string.Equals(ch.CommandId, commandId, StringComparison.OrdinalIgnoreCase)) return ch;
        }
        return null;
    }

    private static bool HasSubwoofer(IList<DetectedChannel> channels)
    {
        for (int i = 0; i < channels.Count; i++)
        {
            if (channels[i] != null && AudysseyHardwareQuirks.IsSubwoofer(channels[i])) return true;
        }
        return false;
    }

    /// <summary>
    /// Front-stage = main L/R/C (and a center sub-flavor if present). Side and
    /// height channels can legitimately have a different crossover from the
    /// fronts so we don't fold them into the spread-check.
    /// </summary>
    private static bool IsFrontStage(string commandId)
    {
        if (string.IsNullOrEmpty(commandId)) return false;
        return commandId.Equals("FL", StringComparison.OrdinalIgnoreCase)
            || commandId.Equals("FR", StringComparison.OrdinalIgnoreCase)
            || commandId.Equals("C", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseDecimal(string s, out decimal value) =>
        decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static int HighestPowerOfTwo(int n)
    {
        int p = 1;
        while (p * 2 <= n) p *= 2;
        return p;
    }

    private static string Fmt(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
