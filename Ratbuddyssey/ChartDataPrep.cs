using System.Globalization;
using System.Numerics;

namespace Ratbuddyssey;

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
}
