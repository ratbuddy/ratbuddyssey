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
        }
    }

    [RelayCommand]
    private void SaveFile()
    {
        string path = CurrentFilePath;
#if DEBUG
        if (!string.IsNullOrEmpty(path))
        {
            path = Path.ChangeExtension(path, ".json");
            CurrentFilePath = path;
        }
#endif
        WriteAppToFile(path);
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

    /// <summary>Public entry point used for drag-drop loading from the host window.</summary>
    public void LoadFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        var info = new FileInfo(filePath);
        if (info.Length == 0 || info.Length > MaxAdyFileBytes)
        {
            Trace.TraceWarning("Refusing to load '{0}': size {1} outside accepted range (1..{2}).",
                filePath, info.Length, MaxAdyFileBytes);
            return;
        }

        string serialized = File.ReadAllText(filePath);
        try
        {
            var parsed = JsonConvert.DeserializeObject<AudysseyMultEQApp>(serialized, AdyReadSettings);
            if (parsed != null)
            {
                AudysseyMultEQApp = parsed;
                CurrentFilePath = filePath;
            }
        }
        catch (JsonException ex)
        {
            Trace.TraceWarning("Failed to parse '{0}' as Audyssey calibration: {1}", filePath, ex.Message);
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
