#nullable disable
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Audyssey.MultEQApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ratbuddyssey.Snapshots;

namespace Ratbuddyssey.Tests;

public class CalibrationSnapshotTests
{
    private static readonly string SamplePath = Path.Combine("sample_ady", "tv36ipal v1.ady");

    private static readonly JsonSerializerSettings ReadSettings = new()
    {
        FloatParseHandling = FloatParseHandling.Decimal,
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 64,
    };

    private static AudysseyMultEQApp LoadSample(out string raw)
    {
        raw = File.ReadAllText(SamplePath);
        var app = JsonConvert.DeserializeObject<AudysseyMultEQApp>(raw, ReadSettings);
        if (app.DetectedChannels == null) app.DetectedChannels = new ObservableCollection<DetectedChannel>();
        return app;
    }

    [FactRequiresSample]
    public void CreateSnapshot_FromLiveModel_StoresIndependentDeepCopy()
    {
        var app = LoadSample(out string raw);
        var mgr = new CalibrationSnapshotManager();
        var snap = mgr.CreateSnapshot("Original", app, raw);

        Assert.NotNull(snap);
        Assert.Equal("Original", snap.Description);
        Assert.NotEqual(Guid.Empty, snap.Id);
        Assert.Single(mgr.GetSnapshots());

        // Mutate the live model; snapshot must be unaffected.
        var firstChannel = app.DetectedChannels.First();
        string originalTrim = firstChannel.TrimAdjustment;
        firstChannel.TrimAdjustment = "9.99";

        var snapshotChannel = snap.GetClonedModel().DetectedChannels.First();
        Assert.Equal(originalTrim, snapshotChannel.TrimAdjustment);
    }

    [FactRequiresSample]
    public void RestoreSnapshot_ReturnsFreshClone_NotSnapshotsCachedInstance()
    {
        var app = LoadSample(out string raw);
        var mgr = new CalibrationSnapshotManager();
        var snap = mgr.CreateSnapshot("Original", app, raw);

        var a = mgr.RestoreSnapshot(snap.Id);
        var b = mgr.RestoreSnapshot(snap.Id);

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotSame(a, b);
        // Mutating one restored copy doesn't affect the next restore.
        a.DetectedChannels.First().TrimAdjustment = "12";
        Assert.NotEqual("12", b.DetectedChannels.First().TrimAdjustment);
        var c = mgr.RestoreSnapshot(snap.Id);
        Assert.NotEqual("12", c.DetectedChannels.First().TrimAdjustment);
    }

    [FactRequiresSample]
    public void Diff_TwoSnapshots_ReportsScalarAndPerChannelChanges()
    {
        var app = LoadSample(out string raw);
        var mgr = new CalibrationSnapshotManager();
        var snap1 = mgr.CreateSnapshot("Original", app, raw);

        // Apply some destructive edits.
        app.DynamicEq = !(app.DynamicEq ?? false);
        app.EnTargetCurveType = (app.EnTargetCurveType ?? 0) == 1 ? 0 : 1;
        var ch = app.DetectedChannels.First();
        ch.TrimAdjustment = "-7.5";
        ch.CustomDistance = (ch.CustomDistance ?? 0m) + 0.42m;

        var snap2 = mgr.CreateSnapshot("After edits", app);

        var diff = mgr.GetDiff(snap1, snap2);
        Assert.Contains(diff, d => d.Property == "DynamicEq");
        Assert.Contains(diff, d => d.Property == "EnTargetCurveType");
        Assert.Contains(diff, d => d.Property == $"{ch.CommandId}.TrimAdjustment" && (string)d.After == "-7.5");
        Assert.Contains(diff, d => d.Property == $"{ch.CommandId}.CustomDistance");
    }

    [FactRequiresSample]
    public void Diff_NoChanges_ReturnsEmpty()
    {
        var app = LoadSample(out string raw);
        var mgr = new CalibrationSnapshotManager();
        var s1 = mgr.CreateSnapshot("a", app, raw);
        var s2 = mgr.CreateSnapshot("b", app, raw);
        Assert.Empty(mgr.GetDiff(s1, s2));
    }

    [FactRequiresSample]
    public void Diff_TargetCurvePoints_DetectsAddRemoveChange()
    {
        var app = LoadSample(out string raw);
        var mgr = new CalibrationSnapshotManager();
        var snap1 = mgr.CreateSnapshot("Original", app, raw);

        var ch = app.DetectedChannels.First();
        // Replace target curve with one fresh point.
        ch.CustomTargetCurvePointsDictionary.Clear();
        ch.CustomTargetCurvePointsDictionary.Add(new MyKeyValuePair("100", "-3"));

        var snap2 = mgr.CreateSnapshot("Curve edits", app);
        var diff = mgr.GetDiff(snap1, snap2);

        Assert.Contains(diff, d => d.Property.StartsWith($"{ch.CommandId}.TargetCurvePoint[", System.StringComparison.Ordinal));
    }

    [FactRequiresSample]
    public void GetSnapshots_ReturnsSnapshotsInCreationOrder()
    {
        var app = LoadSample(out string raw);
        var mgr = new CalibrationSnapshotManager();
        var s1 = mgr.CreateSnapshot("first", app, raw);
        var s2 = mgr.CreateSnapshot("second", app, raw);
        var s3 = mgr.CreateSnapshot("third", app, raw);

        var list = mgr.GetSnapshots();
        Assert.Equal(3, list.Count);
        Assert.Equal(s1.Id, list[0].Id);
        Assert.Equal(s2.Id, list[1].Id);
        Assert.Equal(s3.Id, list[2].Id);
    }

    [FactRequiresSample]
    public void Clear_RemovesAllSnapshots()
    {
        var app = LoadSample(out string raw);
        var mgr = new CalibrationSnapshotManager();
        mgr.CreateSnapshot("a", app, raw);
        mgr.CreateSnapshot("b", app, raw);
        mgr.Clear();
        Assert.Empty(mgr.GetSnapshots());
    }

    [FactRequiresSample]
    public void CreateSnapshot_NullModel_ReturnsNull()
    {
        var mgr = new CalibrationSnapshotManager();
        Assert.Null(mgr.CreateSnapshot("nope", null));
        Assert.Empty(mgr.GetSnapshots());
    }
}
