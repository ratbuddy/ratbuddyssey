using System.Globalization;
using System.Numerics;
using Audyssey.MultEQApp;
using Ratbuddyssey.Features.REW;

namespace Ratbuddyssey;

/// <summary>
/// Selects what time-domain stream the chart pipeline should consume for a channel.
/// </summary>
internal enum ChannelResponseSource
{
    /// <summary>Single mic-position response, looked up by key in <c>ResponseData</c>.</summary>
    SingleMicPosition,

    /// <summary>Element-wise average of every valid mic-position response on the channel.</summary>
    AveragedAcrossMicPositions,
}

/// <summary>
/// Pure helpers used by the chart code-behind to convert raw response samples
/// (string arrays from the .ady JSON) into plot-ready (x, y) data. Extracted
/// from the Window so they can be unit-tested without ScottPlot.
/// </summary>
internal static class ChartDataPrep
{
    /// <summary>Audyssey impulse-response sample rate.</summary>
    public const float SampleRate = 48000f;

    private const NumberStyles SampleNumberStyle = NumberStyles.AllowExponent | NumberStyles.Float;

    /// <summary>
    /// Parse a chirp (impulse) sample stream into (timeMs, amplitude) arrays.
    /// Malformed samples become 0 rather than throwing — a corrupt .ady file should
    /// not crash the chart.
    /// </summary>
    public static (double[] xs, double[] ys) BuildChirpSeries(string[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return (System.Array.Empty<double>(), System.Array.Empty<double>());
        }

        int count = samples.Length;
        float totalTimeMs = 1000f * count / SampleRate;
        var xs = new double[count];
        var ys = new double[count];
        for (int j = 0; j < count; j++)
        {
            xs[j] = j * totalTimeMs / count;
            ys[j] = TryParseDouble(samples[j]);
        }
        return (xs, ys);
    }

    /// <summary>
    /// Parse a frequency-response sample stream into the complex array MathNet's
    /// FFT expects, plus the corresponding linear frequency axis (Hz).
    /// Malformed samples become 0 rather than throwing.
    /// </summary>
    /// <remarks>
    /// Math audit (May 2026):
    ///   * <c>freqs[k] = k / N * Fs</c> is the standard DFT bin frequency for a
    ///     length-N real input sampled at Fs. Caller plots only the first N/2
    ///     bins (the meaningful single-sided half), which is correct.
    ///   * The <c>100 *</c> scaling on the input is an arbitrary display gain.
    ///     A <i>principled</i> single-sided amplitude spectrum would scale the
    ///     post-FFT magnitude by <c>2/N</c> (and divide DC + Nyquist by 2),
    ///     after which <c>20*log10(|X|)</c> gives dBFS. The current code skips
    ///     that normalization, so the absolute dB readings are offset by
    ///     <c>20*log10(100 / (2/N)) = 20*log10(50*N)</c>; only relative shape
    ///     is meaningful. This is fine for a measurement-comparison tool, but
    ///     callers should not treat the y-axis as dBFS.
    ///   * <c>20 * log10(magnitude)</c> in the plotter is correct for an
    ///     amplitude spectrum (10*log10 would be the wrong factor \u2014 that's
    ///     for a power spectrum).
    /// The fractional-octave smoother is the algorithm published by John
    /// Mulcahy (REW); it has been independently reviewed and is widely used.
    /// </remarks>
    public static (Complex[] cValues, double[] freqs) BuildSpectrumInput(string[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return (System.Array.Empty<Complex>(), System.Array.Empty<double>());
        }

        int count = samples.Length;
        var cValues = new Complex[count];
        var freqs = new double[count];
        for (int j = 0; j < count; j++)
        {
            decimal d = TryParseDecimal(samples[j]);
            cValues[j] = 100 * (Complex)d;
            freqs[j] = (double)j / count * SampleRate;
        }
        return (cValues, freqs);
    }

    public static double TryParseDouble(string s)
        => double.TryParse(s, SampleNumberStyle, CultureInfo.InvariantCulture, out double d) ? d : 0d;

    public static decimal TryParseDecimal(string s)
        => decimal.TryParse(s, SampleNumberStyle, CultureInfo.InvariantCulture, out decimal d) ? d : 0m;

    /// <summary>
    /// Plot-ready (frequency Hz, SPL dB) arrays for a REW overlay. Returns
    /// empty arrays for null/empty input. This is a pure data-prep helper —
    /// the chart code-behind is responsible for actually drawing the series.
    /// </summary>
    public static (double[] xs, double[] ys) BuildRewOverlaySeries(RewMeasurement measurement)
    {
        if (measurement?.Points == null || measurement.Points.Count == 0)
        {
            return (System.Array.Empty<double>(), System.Array.Empty<double>());
        }

        int n = measurement.Points.Count;
        var xs = new double[n];
        var ys = new double[n];
        for (int i = 0; i < n; i++)
        {
            xs[i] = measurement.Points[i].FrequencyHz;
            ys[i] = measurement.Points[i].SplDb;
        }
        return (xs, ys);
    }

    /// <summary>
    /// Bundle of plot-ready series for one channel: the existing Audyssey
    /// spectrum input plus an optional REW overlay. Renderer-agnostic; lets
    /// the chart code-behind ask for "everything to draw" in one call without
    /// teaching this layer about ScottPlot.
    /// </summary>
    public sealed class ChannelChartSeries
    {
        public Complex[] AudysseyComplex { get; init; } = System.Array.Empty<Complex>();
        public double[] AudysseyFrequencies { get; init; } = System.Array.Empty<double>();
        public double[] OverlayFrequencies { get; init; } = System.Array.Empty<double>();
        public double[] OverlaySplDb { get; init; } = System.Array.Empty<double>();
        public bool HasOverlay => OverlayFrequencies.Length > 0;
    }

    /// <summary>
    /// Combine an Audyssey channel's frequency-response input with an optional
    /// REW overlay. The Audyssey side reuses <see cref="BuildSpectrumInput"/>;
    /// the overlay side reuses <see cref="BuildRewOverlaySeries"/>.
    /// </summary>
    public static ChannelChartSeries BuildChannelChartSeries(
        DetectedChannel channel,
        string micPositionKey,
        ChannelResponseSource source = ChannelResponseSource.SingleMicPosition,
        RewMeasurement overlay = null)
    {
        string[] samples = GetChannelSamples(channel, micPositionKey, source);
        var (cValues, freqs) = BuildSpectrumInput(samples);
        var (oxs, oys) = BuildRewOverlaySeries(overlay);
        return new ChannelChartSeries
        {
            AudysseyComplex = cValues,
            AudysseyFrequencies = freqs,
            OverlayFrequencies = oxs,
            OverlaySplDb = oys,
        };
    }

    /// <summary>
    /// Resolve a channel + source-mode into the time-domain sample array the
    /// existing <see cref="BuildChirpSeries"/> / <see cref="BuildSpectrumInput"/>
    /// helpers consume.
    ///
    /// <para>
    /// Default behavior (<see cref="ChannelResponseSource.SingleMicPosition"/>)
    /// is the existing PlotLine path: look up <paramref name="micPositionKey"/>
    /// in <c>channel.ResponseData</c>. Returns an empty array when missing,
    /// matching the chart's "no data → no line" behavior.
    /// </para>
    /// <para>
    /// <see cref="ChannelResponseSource.AveragedAcrossMicPositions"/> defers
    /// to <see cref="ResponseAveraging.GetAveragedChannelResponse"/> and
    /// reformats the result back into a string[] so the existing parsers
    /// stay reusable. This is intentionally a thin adapter — chart rendering
    /// is unchanged.
    /// </para>
    /// </summary>
    public static string[] GetChannelSamples(
        DetectedChannel channel,
        string micPositionKey,
        ChannelResponseSource source = ChannelResponseSource.SingleMicPosition)
    {
        if (channel == null) return System.Array.Empty<string>();

        switch (source)
        {
            case ChannelResponseSource.AveragedAcrossMicPositions:
                {
                    double[] avg = ResponseAveraging.GetAveragedChannelResponse(channel);
                    if (avg.Length == 0) return System.Array.Empty<string>();
                    var s = new string[avg.Length];
                    for (int i = 0; i < avg.Length; i++)
                    {
                        // Round-trippable invariant formatting so downstream parsers
                        // reproduce the original double value exactly.
                        s[i] = avg[i].ToString("R", CultureInfo.InvariantCulture);
                    }
                    return s;
                }
            case ChannelResponseSource.SingleMicPosition:
            default:
                {
                    if (channel.ResponseData == null
                        || string.IsNullOrEmpty(micPositionKey)
                        || !channel.ResponseData.TryGetValue(micPositionKey, out var values)
                        || values == null)
                    {
                        return System.Array.Empty<string>();
                    }
                    return values;
                }
        }
    }
}
