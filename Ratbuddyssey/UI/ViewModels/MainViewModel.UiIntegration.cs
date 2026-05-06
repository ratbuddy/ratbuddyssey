using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Audyssey.MultEQApp;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ratbuddyssey.Audio.Analysis;
using Ratbuddyssey.Audio.Curves;
using Ratbuddyssey.Features.REW;
using Ratbuddyssey.Snapshots;
// Disambiguate the static class from the same-named property on this class.
using HouseCurvesLib = Ratbuddyssey.HouseCurves;

namespace Ratbuddyssey;

/// <summary>
/// First-pass UI integration surface for the modernization features:
/// snapshots / diff, calibration analysis warnings, parametric curve controls,
/// REW overlay import, and graph display options. The pre-existing
/// <see cref="MainViewModel"/> is augmented via partial-class additions so the
/// original load/save/dialog plumbing stays untouched.
/// </summary>
public partial class MainViewModel
{
    private static readonly JsonSerializerSettings AdyCopyWriteSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
    };

    // ---- Snapshot UI bridge -------------------------------------------------

    /// <summary>Bindable mirror of <see cref="Snapshots"/>.GetSnapshots() for the UI list.</summary>
    public ObservableCollection<CalibrationSnapshot> SnapshotItems { get; } = new();

    [ObservableProperty]
    private CalibrationSnapshot _selectedSnapshot;

    /// <summary>
    /// Latest before/after diff between <see cref="SelectedSnapshot"/> and the live
    /// model. Populated by <see cref="CompareSelectedSnapshotCommand"/>.
    /// </summary>
    public ObservableCollection<CalibrationDiffEntry> DiffEntries { get; } = new();

    [ObservableProperty]
    private string _diffSummary = string.Empty;

    /// <summary>
    /// Re-syncs the bindable snapshot list with the underlying manager. Call after
    /// any operation that mutates the manager (create / clear / restore).
    /// </summary>
    internal void RefreshSnapshotItems()
    {
        SnapshotItems.Clear();
        foreach (var s in Snapshots.GetSnapshots()) SnapshotItems.Add(s);
        if (SelectedSnapshot != null && !SnapshotItems.Contains(SelectedSnapshot))
        {
            SelectedSnapshot = SnapshotItems.Count > 0 ? SnapshotItems[0] : null;
        }
        else if (SelectedSnapshot == null && SnapshotItems.Count > 0)
        {
            SelectedSnapshot = SnapshotItems[0];
        }
    }

    [RelayCommand]
    private void CreateSnapshot()
    {
        if (AudysseyMultEQApp == null) return;
        string desc = $"Manual snapshot {DateTime.Now:HH:mm:ss}";
        var snap = Snapshots.CreateSnapshot(desc, AudysseyMultEQApp);
        if (snap == null) return;
        SnapshotItems.Add(snap);
        SelectedSnapshot = snap;
        Snapshots.SaveSidecar(CurrentFilePath);
    }

    [RelayCommand]
    private void RestoreSelectedSnapshot()
    {
        if (SelectedSnapshot == null || AudysseyMultEQApp == null) return;
        // Capture current state so the user can flip back if they restore by accident.
        var current = Snapshots.CreateSnapshot("Before restore", AudysseyMultEQApp);
        if (current != null) SnapshotItems.Add(current);
        if (RestoreSnapshot(SelectedSnapshot.Id))
        {
            RunAnalysis();
        }
        Snapshots.SaveSidecar(CurrentFilePath);
    }

    [RelayCommand]
    private void CompareSelectedSnapshot()
    {
        DiffEntries.Clear();
        if (SelectedSnapshot == null || AudysseyMultEQApp == null)
        {
            DiffSummary = "(select a snapshot to compare)";
            return;
        }
        // Diff the selected snapshot against the live model. Use a transient
        // manager so we don't pollute the real snapshot list.
        var transient = new CalibrationSnapshotManager();
        var currentSnap = transient.CreateSnapshot("(current)", AudysseyMultEQApp);
        var entries = CalibrationSnapshotManager.Diff(SelectedSnapshot, currentSnap);
        foreach (var e in entries) DiffEntries.Add(e);
        DiffSummary = entries.Count == 0
            ? $"No differences between '{SelectedSnapshot.Description}' and current."
            : $"{entries.Count} difference(s) — '{SelectedSnapshot.Description}' → current.";
    }

    [RelayCommand]
    private async Task SaveCopyAsync()
    {
        try
        {
            if (AudysseyMultEQApp == null)
            {
                await SafeShowErrorAsync("Save copy", "Open an .ady file first.");
                return;
            }
            string suggested = string.IsNullOrEmpty(CurrentFilePath)
                ? "calibration-copy.ady"
                : Path.GetFileNameWithoutExtension(CurrentFilePath) + "-copy.ady";
            string path = await _dialogs.SaveAdyFileAsAsync(suggested);
            if (string.IsNullOrEmpty(path)) return;
            string serialized = JsonConvert.SerializeObject(AudysseyMultEQApp, AdyCopyWriteSettings);
            File.WriteAllText(path, serialized);
            Trace.TraceInformation("Saved a copy to '{0}'.", path);
        }
        catch (Exception ex)
        {
            Trace.TraceError("SaveCopyAsync failed: {0}", ex);
            await SafeShowErrorAsync("Save copy failed", ex.Message);
        }
    }

    // ---- Calibration analysis warnings -------------------------------------

    private readonly CalibrationAnalyzer _analyzer = new();

    /// <summary>Live calibration warnings; refreshed on load and after destructive edits.</summary>
    public ObservableCollection<CalibrationWarning> Warnings { get; } = new();

    [ObservableProperty]
    private string _warningsSummary = "No file loaded.";

    /// <summary>
    /// Re-runs <see cref="CalibrationAnalyzer"/> over the current model and rebuilds
    /// <see cref="Warnings"/>. Tolerates incomplete data — never throws to the UI.
    /// </summary>
    public void RunAnalysis()
    {
        Warnings.Clear();
        var app = AudysseyMultEQApp;
        if (app == null)
        {
            WarningsSummary = "No file loaded.";
            return;
        }
        try
        {
            foreach (var w in _analyzer.Analyze(app)) Warnings.Add(w);
            WarningsSummary = Warnings.Count == 0
                ? "No warnings."
                : $"{Warnings.Count} warning(s).";
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("RunAnalysis failed: {0}", ex.Message);
            WarningsSummary = "Analysis failed (incomplete data).";
        }
    }

    // ---- Parametric curve controls -----------------------------------------

    [ObservableProperty]
    private double _curveStrengthPercent = 100.0;

    [ObservableProperty]
    private double _curveBassBoostDb;

    [ObservableProperty]
    private double _curveTrebleTiltDb;

    private IReadOnlyList<HouseCurvesLib.Point> _previewCurvePoints;

    /// <summary>
    /// Non-null when the user has clicked Preview on the parametric curve. The chart
    /// code-behind reads this on every redraw and overlays it as a dashed series.
    /// Cleared on apply / load / reset.
    /// </summary>
    public IReadOnlyList<HouseCurvesLib.Point> PreviewCurvePoints
    {
        get => _previewCurvePoints;
        private set
        {
            if (_previewCurvePoints == value) return;
            _previewCurvePoints = value;
            OnPropertyChanged(nameof(PreviewCurvePoints));
        }
    }

    private CurveSettings BuildCurveSettings() => new()
    {
        Strength = CurveStrengthPercent / 100.0,
        BassBoostDb = CurveBassBoostDb,
        TrebleTiltDb = CurveTrebleTiltDb,
    };

    [RelayCommand]
    private void PreviewCurve()
    {
        if (SelectedHouseCurve == null) return;
        PreviewCurvePoints = HouseCurvesLib.GetPresetWithSettings(SelectedHouseCurve, BuildCurveSettings());
    }

    [RelayCommand]
    private void ClearPreviewCurve() => PreviewCurvePoints = null;

    /// <summary>
    /// Auto-recompute the parametric preview overlay whenever the user moves
    /// any of the four inputs that feed it (preset, strength, bass, treble).
    /// Only emits a preview when the result would actually differ from flat
    /// — picking the Flat preset with default sliders leaves the chart clean
    /// rather than drawing a useless dotted line at 0 dB.
    /// </summary>
    private void RefreshAutoPreview()
    {
        if (SelectedHouseCurve == null) { PreviewCurvePoints = null; return; }
        bool nonFlatPreset = !ReferenceEquals(SelectedHouseCurve, HouseCurvesLib.All[0]);
        bool sliderTouched = CurveBassBoostDb != 0 || CurveTrebleTiltDb != 0
            || (nonFlatPreset && CurveStrengthPercent < 100);
        if (!nonFlatPreset && !sliderTouched) { PreviewCurvePoints = null; return; }
        PreviewCurvePoints = HouseCurvesLib.GetPresetWithSettings(SelectedHouseCurve, BuildCurveSettings());
    }

    partial void OnSelectedHouseCurveChanged(HouseCurvesLib.HouseCurve value) => RefreshAutoPreview();
    partial void OnCurveStrengthPercentChanged(double value) => RefreshAutoPreview();
    partial void OnCurveBassBoostDbChanged(double value) => RefreshAutoPreview();
    partial void OnCurveTrebleTiltDbChanged(double value) => RefreshAutoPreview();

    /// <summary>
    /// Writes the parametric curve into <paramref name="channel"/>, taking a snapshot
    /// first so the operation is reversible. Does not mutate the base preset
    /// (<see cref="HouseCurves.GetPresetWithSettings"/> returns a fresh list).
    /// </summary>
    public bool ApplyParametricCurveToSelected(DetectedChannel channel)
    {
        if (channel == null || SelectedHouseCurve == null) return false;
        var modified = HouseCurvesLib.GetPresetWithSettings(SelectedHouseCurve, BuildCurveSettings());
        var temp = new HouseCurvesLib.HouseCurve(
            $"{SelectedHouseCurve.Name} (parametric)", string.Empty, modified);
        CreateSnapshotBeforeChange($"Before parametric curve to {channel.CommandId}");
        RefreshSnapshotItems();
        HouseCurvesLib.ApplyTo(channel, temp);
        IsDirty = true;
        PreviewCurvePoints = null;
        RunAnalysis();
        return true;
    }

    public bool ApplyParametricCurveToAll()
    {
        var app = AudysseyMultEQApp;
        if (app?.DetectedChannels == null || SelectedHouseCurve == null) return false;
        var modified = HouseCurvesLib.GetPresetWithSettings(SelectedHouseCurve, BuildCurveSettings());
        var temp = new HouseCurvesLib.HouseCurve(
            $"{SelectedHouseCurve.Name} (parametric)", string.Empty, modified);
        CreateSnapshotBeforeChange("Before parametric curve to all channels");
        RefreshSnapshotItems();
        HouseCurvesLib.ApplyToAll(app.DetectedChannels, temp);
        IsDirty = true;
        PreviewCurvePoints = null;
        RunAnalysis();
        return true;
    }

    // ---- Graph display options ---------------------------------------------

    /// <summary>Overlay the per-channel averaged response on the chart.</summary>
    [ObservableProperty]
    private bool _showAveragedResponse;

    /// <summary>Overlay any imported REW measurement on the chart.</summary>
    [ObservableProperty]
    private bool _showRewOverlay = true;

    // ---- REW import --------------------------------------------------------

    [ObservableProperty]
    private RewMeasurement _rewMeasurement;

    [ObservableProperty]
    private string _rewSummary = "(no REW import)";

    [RelayCommand]
    private async Task ImportRewAsync()
    {
        try
        {
            string path = await _dialogs.OpenRewTxtFileAsync();
            if (string.IsNullOrEmpty(path)) return;
            var parser = new RewTxtParser();
            var result = parser.ParseFile(path);
            if (!result.Success)
            {
                await SafeShowErrorAsync("REW import failed", result.Error ?? "Unknown parse error.");
                return;
            }
            RewMeasurement = result.Measurement;
            RewSummary = BuildRewSummary(result.Measurement);
            ShowRewOverlay = true;
        }
        catch (Exception ex)
        {
            Trace.TraceError("ImportRewAsync failed: {0}", ex);
            await SafeShowErrorAsync("REW import failed", ex.Message);
        }
    }

    [RelayCommand]
    private void ClearRew()
    {
        RewMeasurement = null;
        RewSummary = "(no REW import)";
    }

    private static string BuildRewSummary(RewMeasurement m)
    {
        if (m?.Points == null || m.Points.Count == 0) return "(empty REW measurement)";
        double minHz = double.PositiveInfinity;
        double maxHz = double.NegativeInfinity;
        for (int i = 0; i < m.Points.Count; i++)
        {
            double f = m.Points[i].FrequencyHz;
            if (f < minHz) minHz = f;
            if (f > maxHz) maxHz = f;
        }
        return string.Format(CultureInfo.InvariantCulture,
            "{0}  •  {1} pts  •  {2:0.#}–{3:0.#} Hz",
            string.IsNullOrEmpty(m.Name) ? "(unnamed)" : m.Name,
            m.Points.Count, minHz, maxHz);
    }
}
