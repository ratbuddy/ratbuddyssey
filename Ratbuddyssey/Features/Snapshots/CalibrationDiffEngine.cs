using System.Collections.Generic;
using System.Linq;
using Audyssey.MultEQApp;

namespace Ratbuddyssey.Snapshots;

/// <summary>
/// First-pass structured diff of two <see cref="AudysseyMultEQApp"/> trees.
///
/// Compares the calibration knobs that matter for "before vs after" review:
/// hardware-level toggles, per-channel trims/distances/crossovers/rolloffs,
/// and target-curve points. This is intentionally NOT a generic JSON diff —
/// the goal is a domain-meaningful list (e.g. "FrontLeft.TrimAdjustment: -2.5 -> -1.0"),
/// not noise from key-order or whitespace changes.
///
/// Channels are paired by <see cref="DetectedChannel.CommandId"/>; channels that
/// only exist in one side are emitted as a single entry with the missing side's
/// value reported as <c>null</c>.
/// </summary>
internal static class CalibrationDiffEngine
{
    public static IReadOnlyList<CalibrationDiffEntry> Diff(AudysseyMultEQApp before, AudysseyMultEQApp after)
    {
        var entries = new List<CalibrationDiffEntry>();
        if (before == null && after == null) return entries;

        // ---------- Top-level scalars ----------
        Compare(entries, "TargetModelName", before?.TargetModelName, after?.TargetModelName);
        Compare(entries, "InterfaceVersion", before?.InterfaceVersion, after?.InterfaceVersion);
        Compare(entries, "EnTargetCurveType", before?.EnTargetCurveType, after?.EnTargetCurveType);
        Compare(entries, "EnAmpAssignType", before?.EnAmpAssignType, after?.EnAmpAssignType);
        Compare(entries, "EnMultEQType", before?.EnMultEQType, after?.EnMultEQType);
        Compare(entries, "DynamicEq", before?.DynamicEq, after?.DynamicEq);
        Compare(entries, "DynamicVolume", before?.DynamicVolume, after?.DynamicVolume);
        Compare(entries, "LfcSupport", before?.LfcSupport, after?.LfcSupport);
        Compare(entries, "Lfc", before?.Lfc, after?.Lfc);
        Compare(entries, "Auro", before?.Auro, after?.Auro);
        Compare(entries, "AdcLineup", before?.AdcLineup, after?.AdcLineup);
        Compare(entries, "SystemDelay", before?.SystemDelay, after?.SystemDelay);
        Compare(entries, "AmpAssignInfo", before?.AmpAssignInfo, after?.AmpAssignInfo);
        Compare(entries, "UpgradeInfo", before?.UpgradeInfo, after?.UpgradeInfo);

        // ---------- Per-channel ----------
        var beforeChannels = (before?.DetectedChannels ?? new System.Collections.ObjectModel.ObservableCollection<DetectedChannel>())
            .Where(c => c != null)
            .ToList();
        var afterChannels = (after?.DetectedChannels ?? new System.Collections.ObjectModel.ObservableCollection<DetectedChannel>())
            .Where(c => c != null)
            .ToList();

        var beforeByKey = ToKeyed(beforeChannels);
        var afterByKey = ToKeyed(afterChannels);

        // Preserve the discovery order of the "after" side, then trail with channels
        // that only exist on the "before" side, so a diff reads roughly top-to-bottom.
        var orderedKeys = new List<string>();
        foreach (var k in afterByKey.Keys) orderedKeys.Add(k);
        foreach (var k in beforeByKey.Keys) if (!afterByKey.ContainsKey(k)) orderedKeys.Add(k);

        foreach (var key in orderedKeys)
        {
            beforeByKey.TryGetValue(key, out var b);
            afterByKey.TryGetValue(key, out var a);
            DiffChannel(entries, key, b, a);
        }

        return entries;
    }

    private static Dictionary<string, DetectedChannel> ToKeyed(List<DetectedChannel> channels)
    {
        var map = new Dictionary<string, DetectedChannel>();
        for (int i = 0; i < channels.Count; i++)
        {
            // CommandId is the natural key in .ady files; fall back to a positional
            // key if it's missing so we still produce *some* diff.
            string key = !string.IsNullOrEmpty(channels[i].CommandId)
                ? channels[i].CommandId
                : $"#index{i}";
            // Don't crash on duplicate command ids; suffix to keep it deterministic.
            string unique = key;
            int suffix = 1;
            while (map.ContainsKey(unique)) unique = key + "~" + (++suffix);
            map[unique] = channels[i];
        }
        return map;
    }

    private static void DiffChannel(List<CalibrationDiffEntry> entries, string label, DetectedChannel b, DetectedChannel a)
    {
        // Channel was added or removed entirely.
        if (b == null || a == null)
        {
            entries.Add(new CalibrationDiffEntry($"{label} (channel)", b == null ? null : "present", a == null ? null : "present"));
            return;
        }

        Compare(entries, $"{label}.TrimAdjustment", b.TrimAdjustment, a.TrimAdjustment);
        Compare(entries, $"{label}.DelayAdjustment", b.DelayAdjustment, a.DelayAdjustment);
        Compare(entries, $"{label}.CustomLevel", b.CustomLevel, a.CustomLevel);
        Compare(entries, $"{label}.CustomDistance", b.CustomDistance, a.CustomDistance);
        Compare(entries, $"{label}.CustomCrossover", b.CustomCrossover, a.CustomCrossover);
        Compare(entries, $"{label}.CustomSpeakerType", b.CustomSpeakerType, a.CustomSpeakerType);
        Compare(entries, $"{label}.FrequencyRangeRolloff", b.FrequencyRangeRolloff, a.FrequencyRangeRolloff);
        Compare(entries, $"{label}.MidrangeCompensation", b.MidrangeCompensation, a.MidrangeCompensation);
        Compare(entries, $"{label}.EnChannelType", b.EnChannelType, a.EnChannelType);
        Compare(entries, $"{label}.IsSkipMeasurement", b.IsSkipMeasurement, a.IsSkipMeasurement);

        DiffTargetCurve(entries, label, b, a);
    }

    private static void DiffTargetCurve(List<CalibrationDiffEntry> entries, string label, DetectedChannel b, DetectedChannel a)
    {
        var beforePts = ToCurveDict(b);
        var afterPts = ToCurveDict(a);
        var keys = new SortedSet<string>(beforePts.Keys);
        foreach (var k in afterPts.Keys) keys.Add(k);
        foreach (var k in keys)
        {
            beforePts.TryGetValue(k, out var bv);
            afterPts.TryGetValue(k, out var av);
            if (!StringEquals(bv, av))
            {
                entries.Add(new CalibrationDiffEntry($"{label}.TargetCurvePoint[{k}]", bv, av));
            }
        }
    }

    private static Dictionary<string, string> ToCurveDict(DetectedChannel ch)
    {
        var d = new Dictionary<string, string>();
        var coll = ch.CustomTargetCurvePointsDictionary;
        if (coll == null) return d;
        foreach (var p in coll)
        {
            if (string.IsNullOrEmpty(p?.Key)) continue;
            d[p.Key] = p.Value;
        }
        return d;
    }

    private static void Compare(List<CalibrationDiffEntry> entries, string property, object before, object after)
    {
        if (!Equals(before, after))
        {
            entries.Add(new CalibrationDiffEntry(property, before, after));
        }
    }

    private static bool StringEquals(string x, string y) => string.Equals(x ?? string.Empty, y ?? string.Empty, System.StringComparison.Ordinal);
}
