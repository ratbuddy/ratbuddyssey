using System;
using Audyssey.MultEQApp;

namespace Ratbuddyssey.Snapshots;

/// <summary>
/// Immutable point-in-time copy of a loaded calibration.
///
/// Lifecycle:
///   1. <see cref="CalibrationSnapshotManager.CreateSnapshot"/> serializes the current
///      <see cref="AudysseyMultEQApp"/> to JSON (the canonical, lossless form for .ady)
///      and also caches a deep-copied parsed instance so diffing doesn't have to
///      re-deserialize each time.
///   2. <see cref="GetClonedModel"/> returns a fresh deep copy on every call so callers
///      can mutate freely without touching the snapshot's own cached object.
///   3. The snapshot itself is never modified after construction. The original
///      live model is therefore unaffected by any later edits.
/// </summary>
public sealed class CalibrationSnapshot
{
    /// <summary>Stable identifier; assigned at creation, never changes.</summary>
    public Guid Id { get; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime TimestampUtc { get; }

    /// <summary>User- or system-supplied description (e.g. "Original", "Before Harman curve").</summary>
    public string Description { get; }

    /// <summary>
    /// Full .ady JSON payload at snapshot time. Kept verbatim so even properties
    /// we don't model (extension data, future schema fields) survive a restore.
    /// </summary>
    public string Json { get; }

    /// <summary>
    /// Cached parsed copy. Treat as read-only; use <see cref="GetClonedModel"/> for
    /// anything that might mutate the tree.
    /// </summary>
    internal AudysseyMultEQApp ParsedModel { get; }

    private readonly Func<string, AudysseyMultEQApp> _deserializer;

    internal CalibrationSnapshot(
        Guid id,
        DateTime timestampUtc,
        string description,
        string json,
        AudysseyMultEQApp parsedModel,
        Func<string, AudysseyMultEQApp> deserializer)
    {
        Id = id;
        TimestampUtc = timestampUtc;
        Description = description ?? string.Empty;
        Json = json ?? string.Empty;
        ParsedModel = parsedModel;
        _deserializer = deserializer;
    }

    /// <summary>
    /// Returns a freshly-deserialized deep copy of the snapshotted model. Safe to
    /// hand to the view model as the new live model, or to pass into a destructive
    /// diff/comparison helper.
    /// </summary>
    public AudysseyMultEQApp GetClonedModel() => _deserializer(Json);
}
