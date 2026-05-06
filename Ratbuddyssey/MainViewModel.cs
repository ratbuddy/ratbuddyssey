using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Audyssey;
using Audyssey.MultEQApp;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Ratbuddyssey;

public partial class MainViewModel : ObservableObject
{
    // Refuse to deserialize anything wildly larger than typical Audyssey calibrations
    // (real-world files are <2 MiB). Defends against zip-bomb-style JSON nesting and
    // accidental drops of huge unrelated files.
    private const long MaxAdyFileBytes = 32L * 1024 * 1024;

    // Pinned defensively even though TypeNameHandling defaults to None.
    private static readonly JsonSerializerSettings AdyReadSettings = new()
    {
        FloatParseHandling = FloatParseHandling.Decimal,
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 64,
    };

    private static readonly JsonSerializerSettings AdyWriteSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
    };

    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private AudysseyMultEQApp _audysseyMultEQApp;

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    /// <summary>
    /// True when the loaded model has been mutated since the last open/save.
    /// Drives the title-bar asterisk and the discard-changes prompt.
    /// </summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>Title shown in the host window. Tracks file name + dirty marker.</summary>
    [ObservableProperty]
    private string _windowTitle = "Ratbuddyssey";

    /// <summary>
    /// Per-channel display rows (decorates <see cref="DetectedChannel"/> with
    /// a friendly speaker name + AudysseyOne-derived hardware-limits validation).
    /// </summary>
    public ObservableCollection<ChannelRowViewModel> ChannelRows { get; } = new();

    /// <summary>Live-filtered view of <see cref="ChannelRows"/> driven by <see cref="ChannelFilter"/>.</summary>
    public ObservableCollection<ChannelRowViewModel> FilteredChannelRows { get; } = new();

    /// <summary>Free-text filter applied to the channel grid (matches commandId or speaker name).</summary>
    [ObservableProperty]
    private string _channelFilter = string.Empty;

    partial void OnChannelFilterChanged(string value) => RebuildFilteredRows();

    /// <summary>
    /// AudysseyOne-style hardware quirks summary for the loaded receiver/file.
    /// Empty when no file is loaded.
    /// </summary>
    [ObservableProperty]
    private string _hardwareInfo = string.Empty;

    /// <summary>Speed of sound (m/s) Audyssey uses on the loaded receiver. 0 when no file is loaded.</summary>
    [ObservableProperty]
    private double _speedOfSoundMps;

    /// <summary>Remaining sub-distance headroom (m) given the per-sub <c>delayAdjustment</c> already applied.</summary>
    [ObservableProperty]
    private decimal _subwooferDelayHeadroomMeters;

    /// <summary>Most-negative trim (dB) AudysseyOne would still let the AVR accept on the sub channel.</summary>
    [ObservableProperty]
    private decimal _subwooferTrimFloorDb;

    /// <summary>Catalogue of "house curve" presets shown in the Target Curve Points section.</summary>
    public IReadOnlyList<Ratbuddyssey.HouseCurves.HouseCurve> HouseCurves => Ratbuddyssey.HouseCurves.All;

    /// <summary>Currently-selected preset; defaults to the flat reference so applying it is a safe no-op.</summary>
    [ObservableProperty]
    private Ratbuddyssey.HouseCurves.HouseCurve _selectedHouseCurve = Ratbuddyssey.HouseCurves.All[0];

    public MainViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    partial void OnAudysseyMultEQAppChanged(AudysseyMultEQApp oldValue, AudysseyMultEQApp newValue)
    {
        if (oldValue != null) UnsubscribeDirtyTracking(oldValue);
        if (newValue != null) SubscribeDirtyTracking(newValue);
        RefreshHardwareQuirks();
        RebuildChannelRows();
        UpdateWindowTitle();
    }

    private void RebuildChannelRows()
    {
        foreach (var row in ChannelRows) row.Dispose();
        ChannelRows.Clear();
        var app = AudysseyMultEQApp;
        if (app?.DetectedChannels != null)
        {
            foreach (var ch in app.DetectedChannels)
            {
                if (ch == null) continue;
                ChannelRows.Add(new ChannelRowViewModel(ch, () => SubwooferTrimFloorDb));
            }
        }
        RebuildFilteredRows();
    }

    private void RebuildFilteredRows()
    {
        FilteredChannelRows.Clear();
        foreach (var r in ChannelRows)
        {
            if (r.MatchesFilter(ChannelFilter)) FilteredChannelRows.Add(r);
        }
    }

    private void RefreshAllRowValidations()
    {
        foreach (var r in ChannelRows) r.Refresh();
    }

    partial void OnCurrentFilePathChanged(string value) => UpdateWindowTitle();
    partial void OnIsDirtyChanged(bool value) => UpdateWindowTitle();
    partial void OnSubwooferTrimFloorDbChanged(decimal value) => RefreshAllRowValidations();

    private void UpdateWindowTitle()
    {
        string name = string.IsNullOrEmpty(CurrentFilePath) ? "(no file)" : Path.GetFileName(CurrentFilePath);
        string mark = IsDirty ? "*" : string.Empty;
        WindowTitle = $"Ratbuddyssey — {name}{mark}";
    }

    private void SubscribeDirtyTracking(AudysseyMultEQApp app)
    {
        app.PropertyChanged += OnModelChanged;
        if (app.DetectedChannels != null)
        {
            app.DetectedChannels.CollectionChanged += OnDetectedChannelsChanged;
            foreach (var ch in app.DetectedChannels)
            {
                if (ch != null) ch.PropertyChanged += OnModelChanged;
            }
        }
    }

    private void UnsubscribeDirtyTracking(AudysseyMultEQApp app)
    {
        app.PropertyChanged -= OnModelChanged;
        if (app.DetectedChannels != null)
        {
            app.DetectedChannels.CollectionChanged -= OnDetectedChannelsChanged;
            foreach (var ch in app.DetectedChannels)
            {
                if (ch != null) ch.PropertyChanged -= OnModelChanged;
            }
        }
    }

    private void OnDetectedChannelsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is Audyssey.MultEQApp.DetectedChannel ch) ch.PropertyChanged -= OnModelChanged;
            }
        }
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is Audyssey.MultEQApp.DetectedChannel ch) ch.PropertyChanged += OnModelChanged;
            }
        }
        IsDirty = true;
    }

    private void OnModelChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Hardware quirks need to refresh when sub trim/delay/model edits happen.
        if (sender is AudysseyMultEQApp
            || e.PropertyName == nameof(Audyssey.MultEQApp.DetectedChannel.DelayAdjustment)
            || e.PropertyName == nameof(Audyssey.MultEQApp.DetectedChannel.TrimAdjustment))
        {
            RefreshHardwareQuirks();
        }
        IsDirty = true;
    }

    private void RefreshHardwareQuirks()
    {
        var app = AudysseyMultEQApp;
        if (app == null)
        {
            HardwareInfo = string.Empty;
            SpeedOfSoundMps = 0;
            SubwooferDelayHeadroomMeters = 0;
            SubwooferTrimFloorDb = 0;
            return;
        }

        SpeedOfSoundMps = AudysseyHardwareQuirks.GetSpeedOfSoundMps(app.TargetModelName);
        SubwooferDelayHeadroomMeters = AudysseyHardwareQuirks.GetSubwooferDelayHeadroomMeters(app.DetectedChannels);
        SubwooferTrimFloorDb = AudysseyHardwareQuirks.GetSubwooferTrimFloorDb(app.DetectedChannels);

        string model = string.IsNullOrEmpty(app.TargetModelName) ? "(unknown receiver)" : app.TargetModelName;
        HardwareInfo = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0}  •  speed of sound {1:0.#} m/s  •  sub delay headroom {2:0.##} m  •  sub trim floor {3:0.##} dB",
            model, SpeedOfSoundMps, SubwooferDelayHeadroomMeters, SubwooferTrimFloorDb);
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        try
        {
            if (IsDirty && !await _dialogs.ConfirmDiscardChangesAsync()) return;
            string path = await _dialogs.OpenAdyFileAsync();
            if (!string.IsNullOrEmpty(path)) LoadFile(path);
        }
        catch (Exception ex)
        {
            Trace.TraceError("OpenFileAsync failed: {0}", ex);
            await SafeShowErrorAsync("Open failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReloadFileAsync()
    {
        try
        {
            if (!await _dialogs.ConfirmReloadAsync()) return;
            if (File.Exists(CurrentFilePath)) LoadFile(CurrentFilePath);
        }
        catch (Exception ex)
        {
            Trace.TraceError("ReloadFileAsync failed: {0}", ex);
            await SafeShowErrorAsync("Reload failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        string path = CurrentFilePath;
#if DEBUG
        if (!string.IsNullOrEmpty(path))
        {
            path = Path.ChangeExtension(path, ".json");
            CurrentFilePath = path;
        }
#endif
        try
        {
            WriteAppToFile(path);
        }
        catch (Exception ex)
        {
            Trace.TraceError("SaveFile failed: {0}", ex);
            await SafeShowErrorAsync("Save failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveFileAsAsync()
    {
        try
        {
            string path = await _dialogs.SaveAdyFileAsAsync(CurrentFilePath);
            if (!string.IsNullOrEmpty(path))
            {
                CurrentFilePath = path;
                WriteAppToFile(path);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("SaveFileAsAsync failed: {0}", ex);
            await SafeShowErrorAsync("Save failed", ex.Message);
        }
    }

    [RelayCommand]
    private void ExitProgram() => _dialogs.RequestExit();

    [RelayCommand]
    private async Task AboutAsync()
    {
        try
        {
            await _dialogs.ShowAboutAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError("AboutAsync failed: {0}", ex);
        }
    }

    /// <summary>
    /// Public entry point used for menu/drag-drop loading. Synchronous because all
    /// the work is local file I/O on the UI thread; user-facing errors are surfaced
    /// via the dialog service (fire-and-forget so the caller doesn't have to await).
    /// </summary>
    public void LoadFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }
        if (!File.Exists(filePath))
        {
            Trace.TraceWarning("LoadFile: '{0}' not found.", filePath);
            _ = SafeShowErrorAsync("File not found", $"'{filePath}' could not be opened.");
            return;
        }

        var info = new FileInfo(filePath);
        if (info.Length == 0)
        {
            Trace.TraceWarning("LoadFile: '{0}' is empty.", filePath);
            _ = SafeShowErrorAsync("Empty file", $"'{Path.GetFileName(filePath)}' is empty.");
            return;
        }
        if (info.Length > MaxAdyFileBytes)
        {
            Trace.TraceWarning("Refusing to load '{0}': size {1} exceeds {2} bytes.",
                filePath, info.Length, MaxAdyFileBytes);
            _ = SafeShowErrorAsync("File too large",
                $"'{Path.GetFileName(filePath)}' is {info.Length / (1024 * 1024)} MiB; the limit is {MaxAdyFileBytes / (1024 * 1024)} MiB.");
            return;
        }

        string serialized;
        try
        {
            serialized = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("LoadFile: could not read '{0}': {1}", filePath, ex.Message);
            _ = SafeShowErrorAsync("Read failed", ex.Message);
            return;
        }

        AudysseyMultEQApp parsed;
        try
        {
            parsed = JsonConvert.DeserializeObject<AudysseyMultEQApp>(serialized, AdyReadSettings);
        }
        catch (JsonException ex)
        {
            Trace.TraceWarning("Failed to parse '{0}' as Audyssey calibration: {1}", filePath, ex.Message);
            _ = SafeShowErrorAsync("Invalid .ady file",
                $"'{Path.GetFileName(filePath)}' is not a valid Audyssey calibration: {ex.Message}");
            return;
        }

        if (parsed == null)
        {
            _ = SafeShowErrorAsync("Invalid .ady file",
                $"'{Path.GetFileName(filePath)}' did not parse to an Audyssey calibration.");
            return;
        }

        NormalizeModel(parsed);
        AudysseyMultEQApp = parsed;
        CurrentFilePath = filePath;
        IsDirty = false;
    }

    /// <summary>
    /// Defensive cleanup of a freshly deserialized model so chart/UI code can rely on
    /// non-null collections. Does not invent data — only replaces nulls with empties.
    /// </summary>
    internal static void NormalizeModel(AudysseyMultEQApp app)
    {
        if (app == null) return;
        if (app.DetectedChannels == null)
        {
            app.DetectedChannels = new System.Collections.ObjectModel.ObservableCollection<Audyssey.MultEQApp.DetectedChannel>();
            return;
        }
        foreach (var ch in app.DetectedChannels)
        {
            if (ch != null && ch.ResponseData == null)
            {
                ch.ResponseData = new System.Collections.Generic.Dictionary<string, string[]>();
            }
        }
    }

    private async Task SafeShowErrorAsync(string title, string message)
    {
        try
        {
            await _dialogs.ShowErrorAsync(title, message);
        }
        catch (Exception ex)
        {
            Trace.TraceError("ShowErrorAsync failed: {0}", ex);
        }
    }

    private void WriteAppToFile(string fileName)
    {
        if (AudysseyMultEQApp == null || string.IsNullOrEmpty(fileName)) return;
        string serialized = JsonConvert.SerializeObject(AudysseyMultEQApp, AdyWriteSettings);
        if (!string.IsNullOrEmpty(serialized))
        {
            IsDirty = false;
            File.WriteAllText(fileName, serialized);
        }
    }
}
