namespace Ratbuddyssey.Snapshots;

/// <summary>
/// One structured difference between two snapshots. Designed for later UI binding
/// (a future "before / after" panel) without having to re-walk the model tree.
///
/// Use <see cref="Property"/> as a dotted path:
///   "TargetModelName"
///   "EnTargetCurveType"
///   "FrontLeft.TrimAdjustment"
///   "Subwoofer 1.CustomCrossover"
///   "FrontLeft.TargetCurvePoint[80]"
/// </summary>
public sealed record CalibrationDiffEntry(string Property, object Before, object After)
{
    public override string ToString() => $"{Property}: {Before} -> {After}";
}
