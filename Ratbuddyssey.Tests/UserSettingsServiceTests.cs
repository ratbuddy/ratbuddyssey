#nullable disable
using System.IO;
using Ratbuddyssey;

namespace Ratbuddyssey.Tests;

public class UserSettingsServiceTests
{
    private static string TempPath()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ratbuddyssey-tests-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    [Fact]
    public void Defaults_AreSystemThemeAndEmptyRecents()
    {
        string path = TempPath();
        var svc = new UserSettingsService(path);

        Assert.Equal("System", svc.Current.Theme);
        Assert.Empty(svc.Current.RecentFiles);
    }

    [Fact]
    public void AddRecentFile_DeduplicatesAndCapsAtEight()
    {
        string path = TempPath();
        var svc = new UserSettingsService(path);

        for (int i = 0; i < 12; i++)
        {
            svc.AddRecentFile($"C:/file{i}.ady");
        }
        // Re-add an existing one — it should move to front, not duplicate.
        svc.AddRecentFile("C:/file7.ady");

        Assert.Equal(UserSettingsService.MaxRecentFiles, svc.Current.RecentFiles.Count);
        Assert.Equal("C:/file7.ady", svc.Current.RecentFiles[0]);
        Assert.Equal(svc.Current.RecentFiles.Count, new System.Collections.Generic.HashSet<string>(svc.Current.RecentFiles).Count);
    }

    [Fact]
    public void Save_RoundTripsThroughDisk()
    {
        string path = TempPath();
        var svc1 = new UserSettingsService(path);
        svc1.Current.Theme = "Dark";
        svc1.AddRecentFile("C:/sample.ady");

        var svc2 = new UserSettingsService(path);
        Assert.Equal("Dark", svc2.Current.Theme);
        Assert.Single(svc2.Current.RecentFiles);
        Assert.Equal("C:/sample.ady", svc2.Current.RecentFiles[0]);
    }

    [Fact]
    public void Load_HandlesMissingFileGracefully()
    {
        string path = Path.Combine(Path.GetTempPath(), "ratbuddyssey-missing-" + System.Guid.NewGuid().ToString("N") + ".json");
        var svc = new UserSettingsService(path);

        Assert.Equal("System", svc.Current.Theme);
    }

    [Fact]
    public void Load_HandlesCorruptFileGracefully()
    {
        string path = TempPath();
        File.WriteAllText(path, "{ this is not json");

        var svc = new UserSettingsService(path);
        Assert.Equal("System", svc.Current.Theme);
    }
}
