using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Audyssey.MultEQApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Ratbuddyssey.Snapshots;

/// <summary>
/// Owns the in-memory list of <see cref="CalibrationSnapshot"/>s for the current
/// session and brokers create / restore / diff. Snapshots live only in memory
/// (no disk persistence yet — that's a follow-up).
///
/// SAFETY CONTRACT
///   * <see cref="CreateSnapshot"/> serializes the live model to JSON and stashes
///     a deep-copied parsed instance. The caller's model is never referenced again.
///   * <see cref="RestoreSnapshot"/> always returns a *fresh* clone of the snapshot,
///     so restoring twice in a row is safe and never mutates a previously-restored
///     snapshot's cached state.
///   * <see cref="GetDiff"/> reads only the snapshots' cached models; it never
///     touches the live model the view model is currently displaying.
/// </summary>
public sealed class CalibrationSnapshotManager
{
    private readonly List<CalibrationSnapshot> _snapshots = new();

    // Same settings the view model uses to read/write .ady, kept in sync defensively.
    private static readonly JsonSerializerSettings WriteSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
    };

    private static readonly JsonSerializerSettings ReadSettings = new()
    {
        FloatParseHandling = FloatParseHandling.Decimal,
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 64,
    };

    /// <summary>Read-only view of all snapshots in creation order.</summary>
    public IReadOnlyList<CalibrationSnapshot> GetSnapshots() => _snapshots.AsReadOnly();

    /// <summary>Removes every snapshot. Called when the user opens a different file.</summary>
    public void Clear() => _snapshots.Clear();

    /// <summary>
    /// Captures <paramref name="liveModel"/> as a new snapshot.
    ///
    /// If <paramref name="rawJson"/> is supplied (e.g. straight from disk on file
    /// load), it's used verbatim — that preserves byte-for-byte fidelity including
    /// extension data we don't model. Otherwise the model is re-serialized.
    /// Returns null when there's nothing to snapshot.
    /// </summary>
    public CalibrationSnapshot CreateSnapshot(string description, AudysseyMultEQApp liveModel, string rawJson = null)
    {
        if (liveModel == null) return null;

        string json = !string.IsNullOrEmpty(rawJson)
            ? rawJson
            : JsonConvert.SerializeObject(liveModel, WriteSettings);

        // Deep copy via round-trip so the snapshot can never be mutated through the
        // live model (and vice versa). Even if rawJson came from disk, we still
        // round-trip so the cached parsed view is internally consistent.
        var parsedCopy = Deserialize(json);

        var snap = new CalibrationSnapshot(
            id: Guid.NewGuid(),
            timestampUtc: DateTime.UtcNow,
            description: description,
            json: json,
            parsedModel: parsedCopy,
            deserializer: Deserialize);

        _snapshots.Add(snap);
        return snap;
    }

    /// <summary>
    /// Returns a fresh deep-copied <see cref="AudysseyMultEQApp"/> reconstituted
    /// from the snapshot. The caller (typically the view model) is responsible
    /// for assigning it to the live model. Returns null if no snapshot matches.
    /// </summary>
    public AudysseyMultEQApp RestoreSnapshot(Guid snapshotId)
    {
        var snap = Find(snapshotId);
        return snap?.GetClonedModel();
    }

    /// <summary>
    /// Structured before/after diff between two snapshots. Either argument may be
    /// null (treated as "empty"). Returns an empty list when no differences exist.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Public manager API surface; static counterpart is Diff().")]
    public IReadOnlyList<CalibrationDiffEntry> GetDiff(CalibrationSnapshot snapshotA, CalibrationSnapshot snapshotB)
    {
        return CalibrationDiffEngine.Diff(snapshotA?.ParsedModel, snapshotB?.ParsedModel);
    }

    /// <summary>Convenience: diff by snapshot id.</summary>
    public IReadOnlyList<CalibrationDiffEntry> GetDiff(Guid snapshotIdA, Guid snapshotIdB)
        => GetDiff(Find(snapshotIdA), Find(snapshotIdB));

    /// <summary>
    /// Static overload — pure function over two snapshots, callable without a manager.
    /// </summary>
    public static IReadOnlyList<CalibrationDiffEntry> Diff(CalibrationSnapshot a, CalibrationSnapshot b)
        => CalibrationDiffEngine.Diff(a?.ParsedModel, b?.ParsedModel);

    private CalibrationSnapshot Find(Guid id)
    {
        for (int i = 0; i < _snapshots.Count; i++)
        {
            if (_snapshots[i].Id == id) return _snapshots[i];
        }
        return null;
    }

    private static AudysseyMultEQApp Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var app = JsonConvert.DeserializeObject<AudysseyMultEQApp>(json, ReadSettings);
        if (app != null && app.DetectedChannels == null)
        {
            app.DetectedChannels = new ObservableCollection<DetectedChannel>();
        }
        return app;
    }

    /// <summary>
    /// Returns the canonical sidecar path for the snapshots of <paramref name="adyPath"/>.
    /// We append <c>.snapshots.json</c> rather than swapping the extension so the file
    /// is obviously paired with the .ady (and so accidental double-clicks on the sidecar
    /// don't try to launch the app via the .ady file association).
    /// </summary>
    public static string GetSidecarPath(string adyPath) =>
        string.IsNullOrEmpty(adyPath) ? null : adyPath + ".snapshots.json";

    /// <summary>
    /// On-disk shape for a single snapshot entry. Compact and round-trippable;
    /// the deep copy / diff machinery rebuilds <see cref="CalibrationSnapshot.ParsedModel"/>
    /// on load so we don't need to persist anything else.
    /// </summary>
    private sealed class SidecarEntry
    {
        public Guid Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Description { get; set; }
        public string Json { get; set; }
    }

    /// <summary>
    /// Writes every snapshot except the seeded "Original" to <paramref name="adyPath"/>'s
    /// sidecar file. We skip "Original" because it's regenerated verbatim from the
    /// .ady itself on every load — persisting it would just duplicate the entire
    /// calibration in JSON inside another JSON wrapper for no benefit.
    /// Failures are logged and swallowed: snapshots are a convenience layer and
    /// should never block calibration edits.
    /// </summary>
    public void SaveSidecar(string adyPath)
    {
        string path = GetSidecarPath(adyPath);
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var entries = new List<SidecarEntry>(_snapshots.Count);
            for (int i = 0; i < _snapshots.Count; i++)
            {
                var s = _snapshots[i];
                if (i == 0 && s.Description == "Original") continue;
                entries.Add(new SidecarEntry
                {
                    Id = s.Id,
                    TimestampUtc = s.TimestampUtc,
                    // Cap description length so a runaway caller can't bloat
                    // the sidecar past sensible bounds (the description is only
                    // ever shown in a list row).
                    Description = (s.Description?.Length > 256 ? s.Description.Substring(0, 256) : s.Description) ?? string.Empty,
                    Json = s.Json,
                });
            }
            if (entries.Count == 0)
            {
                // No user-created snapshots — remove any stale sidecar so the
                // file system reflects "nothing here" rather than a previous run's leftovers.
                if (File.Exists(path)) File.Delete(path);
                return;
            }
            // Use the same write settings as the snapshot store itself: explicit
            // TypeNameHandling.None, no $type metadata, no MissingMemberHandling
            // that could throw on a future schema bump.
            string serialized = JsonConvert.SerializeObject(entries, Formatting.Indented, WriteSettings);
            File.WriteAllText(path, serialized);
        }
        catch (Exception ex)
        {
            // Log only the filename, never the full path — Trace output may be
            // captured by debuggers or user-shared bug reports and the parent
            // directory tree often contains the user's name / private folders.
            Trace.TraceWarning("SaveSidecar failed for '{0}': {1}", Path.GetFileName(path), ex.Message);
        }
    }

    /// <summary>
    /// Appends snapshots from <paramref name="adyPath"/>'s sidecar (if any) to the
    /// current list. Does not clear existing entries; callers that want a clean
    /// load should call <see cref="Clear"/> and seed "Original" themselves first.
    /// Returns the number of snapshots loaded (0 when the sidecar is missing or unreadable).
    /// </summary>
    public int LoadSidecar(string adyPath)
    {
        const long maxSidecarBytes = 64L * 1024 * 1024; // 64 MB hard ceiling — well above any realistic snapshot history
        string path = GetSidecarPath(adyPath);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
        try
        {
            // Refuse to read a sidecar that's been tampered with or grown
            // pathologically large; protects against a malicious .ady-paired
            // sidecar dropping the app into a multi-gigabyte allocation.
            var info = new FileInfo(path);
            if (info.Length > maxSidecarBytes)
            {
                Trace.TraceWarning("LoadSidecar refused '{0}': sidecar exceeds {1} byte ceiling.", Path.GetFileName(path), maxSidecarBytes);
                return 0;
            }
            string text = File.ReadAllText(path);
            // Pin TypeNameHandling.None explicitly. Newtonsoft's default already
            // disables polymorphic deserialization, but stating it here means a
            // future global default change can't silently widen the attack
            // surface for a known on-disk format.
            var entries = JsonConvert.DeserializeObject<List<SidecarEntry>>(text, ReadSettings);
            if (entries == null || entries.Count == 0) return 0;
            int loaded = 0;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e?.Json)) continue;
                var parsed = Deserialize(e.Json);
                if (parsed == null) continue;
                _snapshots.Add(new CalibrationSnapshot(
                    id: e.Id == Guid.Empty ? Guid.NewGuid() : e.Id,
                    timestampUtc: e.TimestampUtc == default ? DateTime.UtcNow : e.TimestampUtc,
                    description: e.Description ?? string.Empty,
                    json: e.Json,
                    parsedModel: parsed,
                    deserializer: Deserialize));
                loaded++;
            }
            return loaded;
        }
        catch (Exception ex)
        {
            // See the SaveSidecar comment: log filename only, never the full path.
            Trace.TraceWarning("LoadSidecar failed for '{0}': {1}", Path.GetFileName(path), ex.Message);
            return 0;
        }
    }
}
