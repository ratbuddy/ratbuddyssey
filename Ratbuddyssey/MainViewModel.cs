using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

    public MainViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        try
        {
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
            File.WriteAllText(fileName, serialized);
        }
    }
}
