using System;
using System.Globalization;
using System.Text;
using Audyssey.MultEQApp;

namespace Audyssey;

/// <summary>
/// Severity of a per-channel hardware-limits validation.
/// </summary>
public enum ValidationSeverity
{
    Ok,
    Warning,
    Error,
}

/// <summary>
/// Result of validating a single <see cref="DetectedChannel"/> against the
/// AVR's known hardware limits (clamped trim/level, valid crossover snap, etc.).
/// </summary>
public readonly record struct ChannelValidation(ValidationSeverity Severity, string Message)
{
    public static ChannelValidation Ok { get; } = new(ValidationSeverity.Ok, "");
}

/// <summary>
/// Validates a per-channel slice of a loaded calibration against the
/// AudysseyOne-derived limits. Pure function on the channel + the hardware
/// quirks summary computed at the parent level.
/// </summary>
public static class ChannelLimitsValidator
{
    private const decimal MaxTrimDb = AudysseyHardwareQuirks.MaxAbsoluteTrimDb;

    public static ChannelValidation Validate(DetectedChannel channel, decimal subwooferTrimFloorDb)
    {
        if (channel == null) return ChannelValidation.Ok;

        bool isSub = AudysseyHardwareQuirks.IsSubwoofer(channel);

        // 1. Custom level vs trim limits. Subwoofer floor is dictated by what
        //    the AVR has already burned into trimAdjustment; sats are simple ±12.
        if (TryParse(channel.CustomLevel, out decimal level))
        {
            decimal floor = isSub ? subwooferTrimFloorDb : -MaxTrimDb;
            decimal ceiling = MaxTrimDb;
            if (level < floor || level > ceiling)
            {
                return new ChannelValidation(
                    ValidationSeverity.Error,
                    Format("customLevel {0:0.##} dB is outside the receiver's allowed [{1:0.##}, {2:0.##}] dB range.",
                        level, floor, ceiling));
            }
            // Soft warning if this sub level can't actually be reached after
            // adding the existing trimAdjustment (clip on the second knob).
            if (isSub && TryParse(channel.TrimAdjustment, out decimal trim))
            {
                decimal effective = level + trim;
                if (effective > MaxTrimDb || effective < -MaxTrimDb)
                {
                    return new ChannelValidation(
                        ValidationSeverity.Warning,
                        Format("customLevel + trimAdjustment ({0:0.##} dB) clips the receiver's ±{1:0.##} dB range.",
                            effective, MaxTrimDb));
                }
            }
        }

        // 2. Crossover must be one of the receiver's recognized values
        //    (the editor will refuse anything else).
        if (!isSub && !string.IsNullOrEmpty(channel.CustomCrossover))
        {
            if (!int.TryParse(channel.CustomCrossover, NumberStyles.Integer, CultureInfo.InvariantCulture, out int xo))
            {
                if (!string.Equals(channel.CustomCrossover, "F", StringComparison.OrdinalIgnoreCase))
                {
                    return new ChannelValidation(
                        ValidationSeverity.Warning,
                        $"customCrossover '{channel.CustomCrossover}' is not a recognized receiver value.");
                }
            }
            else
            {
                bool found = false;
                foreach (int valid in AudysseyHardwareQuirks.ValidCrossoverFrequenciesHz)
                {
                    if (valid == xo) { found = true; break; }
                }
                if (!found)
                {
                    return new ChannelValidation(
                        ValidationSeverity.Warning,
                        $"customCrossover {xo} Hz is not in the receiver's snap list (40, 60, 80, 90, 100, 110, 120, 150, 180, 200, 250).");
                }
                if (xo < AudysseyHardwareQuirks.MinimumCrossoverHz)
                {
                    return new ChannelValidation(
                        ValidationSeverity.Warning,
                        $"customCrossover {xo} Hz is below AudysseyOne's recommended {AudysseyHardwareQuirks.MinimumCrossoverHz} Hz floor.");
                }
            }
        }

        return ChannelValidation.Ok;
    }

    private static bool TryParse(string s, out decimal value)
        => decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static string Format(string fmt, params object[] args)
        => string.Format(CultureInfo.InvariantCulture, fmt, args);
}
