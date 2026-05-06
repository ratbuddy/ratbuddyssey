using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Audyssey.MultEQApp;

namespace Audyssey;

/// <summary>
/// Centralizes Audyssey / AV-receiver hardware quirks the optimization community
/// has documented but the .ady file format does not expose directly.
/// <para>
/// Logic mirrored from the AudysseyOne script
/// (<see href="https://github.com/ObsessiveCompulsiveAudiophile/AudysseyOne"/>),
/// specifically the <c>checkModelSpeedOfSound</c>, <c>updateConstants</c>, and
/// crossover-snapping helpers. Keep this list in sync with that script.
/// </para>
/// </summary>
public static class AudysseyHardwareQuirks
{
    /// <summary>Standard speed of sound (m/s) used by most modern receivers for distance math.</summary>
    public const double SpeedOfSoundDefaultMps = 343.0;

    /// <summary>Legacy speed of sound (m/s) Audyssey hard-codes on the receiver families listed in <see cref="LegacySpeedOfSoundModelSuffixes"/>.</summary>
    public const double SpeedOfSoundLegacyMps = 300.0;

    /// <summary>Sub trim is clamped by the AVR to ±12 dB (per AudysseyOne).</summary>
    public const decimal MaxAbsoluteTrimDb = 12.0m;

    /// <summary>Hard ceiling on per-channel delay adjustment headroom (meters-of-delay-equivalent) used by AudysseyOne.</summary>
    public const decimal MaxSubwooferDelayHeadroomMeters = 6.0m;

    /// <summary>
    /// AudysseyOne's enforced minimum crossover (Hz). The script raises any
    /// computed crossover below 80 Hz to 80 to "minimize driver loads."
    /// </summary>
    public const int MinimumCrossoverHz = 80;

    /// <summary>
    /// Receiver model-name suffixes (last 6 characters of <c>targetModelName</c>)
    /// known to use 300 m/s for Audyssey distance calculations instead of 343 m/s.
    /// Sourced from AudysseyOne's <c>specialModels</c> list.
    /// </summary>
    public static readonly IReadOnlyList<string> LegacySpeedOfSoundModelSuffixes = new ReadOnlyCollection<string>(new[]
    {
        // Denon AVR-X x300 / x400 / x500 / x600 / x700
        "X1300W", "X2300W", "X3300W", "X4300H", "X6300H",
        "X1400H", "X2400H", "X3400H", "X4400H", "X6400H",
        "X1500H", "X2500H", "X3500H", "X4500H", "X6500H", "X8500H",
        "X1600H", "X2600H", "X3600H",
        "X1700H", "X2700H", "X3700H", "X4700H", "X6700H",

        // Marantz SR x011..x015
        "SR5011", "SR6011", "SR7011",
        "SR5012", "SR6012", "SR7012", "SR8012",
        "SR5013", "SR6013", "SR7013",
        "SR5014", "SR6014",
        "SR5015", "SR6015", "SR7015", "SR8015",

        // Marantz AV pre-amps + Denon AVR slim line
        "AV7703", "AV7704", "AV7705", "AV7706", "AV8805",
        "NR1607", "NR1608", "NR1711",
    });

    /// <summary>
    /// Valid Audyssey crossover frequencies (Hz) the receiver UI exposes,
    /// excluding the 'Full Range' sentinel.
    /// </summary>
    public static readonly IReadOnlyList<int> ValidCrossoverFrequenciesHz = new ReadOnlyCollection<int>(new[]
    {
        40, 60, 80, 90, 100, 110, 120, 150, 180, 200, 250,
    });

    /// <summary>
    /// Returns the speed of sound (m/s) Audyssey will use on the named receiver.
    /// Matches AudysseyOne's <c>checkModelSpeedOfSound</c>: the last 6
    /// characters of <paramref name="targetModelName"/> are matched against
    /// <see cref="LegacySpeedOfSoundModelSuffixes"/>.
    /// </summary>
    public static double GetSpeedOfSoundMps(string targetModelName)
    {
        if (string.IsNullOrEmpty(targetModelName) || targetModelName.Length < 6)
        {
            return SpeedOfSoundDefaultMps;
        }
        string suffix = targetModelName.Substring(targetModelName.Length - 6);
        foreach (string known in LegacySpeedOfSoundModelSuffixes)
        {
            if (string.Equals(known, suffix, StringComparison.OrdinalIgnoreCase))
            {
                return SpeedOfSoundLegacyMps;
            }
        }
        return SpeedOfSoundDefaultMps;
    }

    /// <summary>
    /// Snaps an arbitrary crossover frequency to the nearest value the receiver
    /// will accept, enforcing AudysseyOne's 80 Hz floor.
    /// </summary>
    public static int SnapCrossoverHz(double rawCrossoverHz)
        => SnapCrossoverHz(rawCrossoverHz, MinimumCrossoverHz);

    /// <summary>
    /// Snaps an arbitrary crossover frequency to the nearest value the receiver
    /// will accept, with a configurable lower bound. Use the default-arg overload
    /// to inherit AudysseyOne's recommended 80 Hz floor.
    /// </summary>
    public static int SnapCrossoverHz(double rawCrossoverHz, int minimumHz)
    {
        int floor = minimumHz;
        int best = floor;
        double bestDiff = double.PositiveInfinity;
        foreach (int xo in ValidCrossoverFrequenciesHz)
        {
            if (xo < floor) continue;
            double diff = Math.Abs(rawCrossoverHz - xo);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = xo;
            }
        }
        return best;
    }

    /// <summary>
    /// Identifies subwoofer entries by their <c>commandId</c> prefix, matching
    /// the convention used by both the official Editor (SW1, SW2, ...) and
    /// AudysseyOne's <c>commandId.startsWith("SW")</c> probe.
    /// </summary>
    public static bool IsSubwoofer(DetectedChannel channel) =>
        channel?.CommandId != null
        && channel.CommandId.StartsWith("SW", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Computes the receiver's remaining subwoofer-delay headroom (meters-equivalent)
    /// given the per-sub <c>delayAdjustment</c> values already applied by
    /// Audyssey. Mirrors AudysseyOne's <c>subtrueMaxDelay = 6.0 - max(delayAdjustment)</c>.
    /// </summary>
    public static decimal GetSubwooferDelayHeadroomMeters(IEnumerable<DetectedChannel> channels)
    {
        if (channels == null) return MaxSubwooferDelayHeadroomMeters;
        decimal worstDelay = 0m;
        foreach (var ch in channels)
        {
            if (!IsSubwoofer(ch)) continue;
            if (TryParseDecimal(ch.DelayAdjustment, out decimal d) && d > worstDelay)
            {
                worstDelay = d;
            }
        }
        return MaxSubwooferDelayHeadroomMeters - worstDelay;
    }

    /// <summary>
    /// Computes the floor (most-negative allowable trim, dB) AudysseyOne uses
    /// when it rebalances the subwoofer level. Mirrors
    /// <c>subtrueMaxTrim = -(12 + min(trimAdjustment)) / 2</c>.
    /// </summary>
    public static decimal GetSubwooferTrimFloorDb(IEnumerable<DetectedChannel> channels)
    {
        if (channels == null) return -(MaxAbsoluteTrimDb) / 2m;
        decimal worstTrim = 0m;
        foreach (var ch in channels)
        {
            if (!IsSubwoofer(ch)) continue;
            if (TryParseDecimal(ch.TrimAdjustment, out decimal t) && t < worstTrim)
            {
                worstTrim = t;
            }
        }
        return -(MaxAbsoluteTrimDb + worstTrim) / 2m;
    }

    private static bool TryParseDecimal(string s, out decimal value)
    {
        if (!string.IsNullOrEmpty(s)
            && decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }
        value = 0m;
        return false;
    }
}
