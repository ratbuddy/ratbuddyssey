using System.Collections.Generic;
using System.Diagnostics;
using Audyssey.MultEQApp;

namespace Ratbuddyssey;

/// <summary>
/// Pure helpers for combining the per-mic-position response samples on a
/// <see cref="DetectedChannel"/> into a single averaged response.
///
/// Audyssey stores up to eight mic positions per channel under
/// <c>DetectedChannel.ResponseData</c> keyed by position index ("0".."7").
/// Each value is a string[] of time-domain chirp samples. Averaging by
/// sample index is the simplest sensible aggregation: if the impulses are
/// already aligned (Audyssey trims them so the main impulse sits at a
/// known offset) it cancels position-dependent room noise while preserving
/// the channel's underlying response.
///
/// This module deliberately stays in the time domain — frequency-domain
/// averaging would require an FFT per position and is left to a follow-up.
/// </summary>
public static class ResponseAveraging
{
    /// <summary>
    /// Average a set of equally-sampled response streams element-wise.
    ///
    /// Behavior:
    ///   * null / empty entries are skipped (defensive against malformed .ady).
    ///   * the output length is the shortest valid stream's length, so a
    ///     single short capture won't truncate the rest down to zero only
    ///     when it itself is non-empty.
    ///   * returns an empty array when no usable input exists.
    ///
    /// The samples are parsed via <see cref="ChartDataPrep.TryParseDouble"/>
    /// equivalents elsewhere; this overload assumes the caller has already
    /// converted to <see cref="double"/>. The string-overload is below.
    /// </summary>
    public static double[] AverageResponses(IEnumerable<double[]> responses)
    {
        if (responses == null) return System.Array.Empty<double>();

        var usable = new List<double[]>();
        int minLen = int.MaxValue;
        foreach (var r in responses)
        {
            if (r == null || r.Length == 0) continue;
            usable.Add(r);
            if (r.Length < minLen) minLen = r.Length;
        }
        if (usable.Count == 0 || minLen == int.MaxValue || minLen == 0)
        {
            return System.Array.Empty<double>();
        }

        var avg = new double[minLen];
        for (int i = 0; i < usable.Count; i++)
        {
            var r = usable[i];
            for (int j = 0; j < minLen; j++) avg[j] += r[j];
        }
        double inv = 1.0 / usable.Count;
        for (int j = 0; j < minLen; j++) avg[j] *= inv;
        return avg;
    }

    /// <summary>
    /// String-input overload that mirrors the on-disk format. Each inner
    /// array is parsed sample-by-sample with <see cref="ChartDataPrep.TryParseDouble"/>
    /// so malformed tokens become 0 rather than throwing.
    /// </summary>
    public static double[] AverageResponses(IEnumerable<string[]> responses)
    {
        if (responses == null) return System.Array.Empty<double>();
        var converted = new List<double[]>();
        foreach (var s in responses)
        {
            if (s == null || s.Length == 0) continue;
            var d = new double[s.Length];
            for (int i = 0; i < s.Length; i++) d[i] = ChartDataPrep.TryParseDouble(s[i]);
            converted.Add(d);
        }
        return AverageResponses((IEnumerable<double[]>)converted);
    }

    /// <summary>
    /// Produce one averaged time-domain response for <paramref name="channel"/>
    /// across all of its mic positions. Returns an empty array when the
    /// channel has no usable response data.
    ///
    /// Logs (at <see cref="Trace"/> info level, the project's existing style)
    /// the channel's command id, how many mic positions contributed, and
    /// the resulting sample count — useful when a user reports "the average
    /// looks weird" and we need to know how many positions actually fed it.
    /// </summary>
    public static double[] GetAveragedChannelResponse(DetectedChannel channel)
    {
        if (channel == null || channel.ResponseData == null || channel.ResponseData.Count == 0)
        {
            return System.Array.Empty<double>();
        }

        var positions = new List<string[]>(channel.ResponseData.Count);
        foreach (var kv in channel.ResponseData)
        {
            // Skip null / empty arrays here so the count we log matches what
            // actually contributed to the average.
            if (kv.Value != null && kv.Value.Length > 0) positions.Add(kv.Value);
        }

        var avg = AverageResponses(positions);
        Trace.TraceInformation(
            "GetAveragedChannelResponse: channel={0}, positions={1}, samples={2}",
            string.IsNullOrEmpty(channel.CommandId) ? "(unnamed)" : channel.CommandId,
            positions.Count,
            avg.Length);
        return avg;
    }
}
