using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace Ratbuddyssey;

/// <summary>
/// Persisted user preferences (theme, recent files, ...).
/// Plain DTO so Newtonsoft can round-trip it without ceremony.
/// </summary>
public sealed class UserSettings
{
    /// <summary>One of "Light", "Dark", "System". Defaults to system.</summary>
    public string Theme { get; set; } = "System";

    /// <summary>MRU list of absolute paths, newest first, max <see cref="UserSettingsService.MaxRecentFiles"/>.</summary>
    public List<string> RecentFiles { get; set; } = new();
}

/// <summary>
/// Loads/saves <see cref="UserSettings"/> to a per-user JSON file.
/// </summary>
public interface IUserSettingsService
{
    UserSettings Current { get; }
    void Save();
    void AddRecentFile(string path);
    void RemoveRecentFile(string path);
}

public sealed class UserSettingsService : IUserSettingsService
{
    public const int MaxRecentFiles = 8;
    private const long MaxSettingsFileBytes = 256 * 1024;

    private readonly string _path;

    public UserSettings Current { get; private set; } = new();

    public UserSettingsService() : this(GetDefaultPath()) { }

    internal UserSettingsService(string path)
    {
        _path = path;
        Current = Load(path) ?? new UserSettings();
    }

    public void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText(_path, json);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("UserSettingsService.Save failed: {0}", ex.Message);
        }
    }

    public void AddRecentFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var list = Current.RecentFiles ??= new List<string>();
        // Case-insensitive de-dup on Windows; ordinal elsewhere.
        var cmp = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (cmp.Equals(list[i], path)) list.RemoveAt(i);
        }
        list.Insert(0, path);
        while (list.Count > MaxRecentFiles) list.RemoveAt(list.Count - 1);
        Save();
    }

    public void RemoveRecentFile(string path)
    {
        if (string.IsNullOrEmpty(path) || Current.RecentFiles == null) return;
        var cmp = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        Current.RecentFiles.RemoveAll(p => cmp.Equals(p, path));
        Save();
    }

    internal static UserSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var info = new FileInfo(path);
            if (info.Length == 0 || info.Length > MaxSettingsFileBytes) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UserSettings>(json);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("UserSettingsService.Load failed: {0}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Returns <c>%APPDATA%\Ratbuddyssey\settings.json</c> on Windows and
    /// <c>~/.config/ratbuddyssey/settings.json</c> elsewhere.
    /// </summary>
    public static string GetDefaultPath()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }
        string folder = OperatingSystem.IsWindows() ? "Ratbuddyssey" : "ratbuddyssey";
        return Path.Combine(baseDir, folder, "settings.json");
    }
}
