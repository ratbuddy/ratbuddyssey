namespace Ratbuddyssey.Audio.Analysis;

/// <summary>
/// Triage level for a calibration warning. Mirrors the existing
/// <c>ChannelLimitsValidator</c> severity scheme but lives in its own
/// namespace so the analyzer can carry its own vocabulary.
/// </summary>
public enum CalibrationWarningSeverity
{
    /// <summary>FYI; nothing actionable but worth surfacing.</summary>
    Info,

    /// <summary>Likely calibration issue; user should review.</summary>
    Warning,

    /// <summary>Hard limit violated or an integration problem that breaks playback.</summary>
    Critical,
}

/// <summary>
/// One finding produced by <c>CalibrationAnalyzer</c>.
///
/// <para>
/// <see cref="Code"/> is a stable, machine-readable identifier
/// (e.g. <c>"trim.outOfRange"</c>) so a future UI / test asserts can
/// match warnings without doing string-search on the human message.
/// </para>
/// <para>
/// <see cref="Channel"/> is the offending channel's <c>commandId</c>
/// (e.g. <c>"FL"</c>, <c>"SW1"</c>) or <c>null</c> for whole-system rules.
/// </para>
/// </summary>
public sealed record CalibrationWarning(
    string Code,
    CalibrationWarningSeverity Severity,
    string Channel,
    string Message)
{
    public override string ToString()
    {
        string scope = string.IsNullOrEmpty(Channel) ? "(system)" : Channel;
        return $"[{Severity}] {Code} {scope}: {Message}";
    }
}
