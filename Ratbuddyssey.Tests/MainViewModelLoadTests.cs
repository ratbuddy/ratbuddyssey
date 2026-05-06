using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Audyssey.MultEQApp;
using Newtonsoft.Json;
using Ratbuddyssey;
using Xunit;

namespace Ratbuddyssey.Tests;

public class MainViewModelLoadTests
{
    private sealed class FakeDialogs : IDialogService
    {
        public List<(string Title, string Message)> Errors { get; } = new();
        public Task<string> OpenAdyFileAsync() => Task.FromResult<string>(null!);
        public Task<string> SaveAdyFileAsAsync(string suggestedName) => Task.FromResult<string>(null!);
        public Task<bool> ConfirmReloadAsync() => Task.FromResult(false);
        public Task ShowAboutAsync() => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message)
        {
            Errors.Add((title, message));
            return Task.CompletedTask;
        }
        public void RequestExit() { }
    }

    [Fact]
    public void LoadFile_MissingPath_ReportsError()
    {
        var dlg = new FakeDialogs();
        var vm = new MainViewModel(dlg);

        vm.LoadFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ady"));

        Assert.Single(dlg.Errors);
        Assert.Equal("File not found", dlg.Errors[0].Title);
        Assert.Null(vm.AudysseyMultEQApp);
    }

    [Fact]
    public void LoadFile_EmptyFile_ReportsError()
    {
        var dlg = new FakeDialogs();
        var vm = new MainViewModel(dlg);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ady");
        File.WriteAllText(path, string.Empty);
        try
        {
            vm.LoadFile(path);
            Assert.Single(dlg.Errors);
            Assert.Equal("Empty file", dlg.Errors[0].Title);
            Assert.Null(vm.AudysseyMultEQApp);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadFile_CorruptJson_ReportsError()
    {
        var dlg = new FakeDialogs();
        var vm = new MainViewModel(dlg);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ady");
        File.WriteAllText(path, "{not valid json");
        try
        {
            vm.LoadFile(path);
            Assert.Single(dlg.Errors);
            Assert.Equal("Invalid .ady file", dlg.Errors[0].Title);
            Assert.Null(vm.AudysseyMultEQApp);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadFile_ValidJsonWithMissingDetectedChannels_NormalizesToEmpty()
    {
        var dlg = new FakeDialogs();
        var vm = new MainViewModel(dlg);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ady");
        // Minimal valid JSON object — no detectedChannels field at all.
        File.WriteAllText(path, "{\"title\":\"t\"}");
        try
        {
            vm.LoadFile(path);
            Assert.Empty(dlg.Errors);
            Assert.NotNull(vm.AudysseyMultEQApp);
            Assert.NotNull(vm.AudysseyMultEQApp.DetectedChannels);
            Assert.Empty(vm.AudysseyMultEQApp.DetectedChannels);
            Assert.Equal(path, vm.CurrentFilePath);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void NormalizeModel_FillsNullResponseDataPerChannel()
    {
        var app = new AudysseyMultEQApp
        {
            DetectedChannels = new System.Collections.ObjectModel.ObservableCollection<DetectedChannel>
            {
                new DetectedChannel(),
            }
        };
        Assert.Null(app.DetectedChannels[0].ResponseData);

        // Use reflection-free path: serialize then re-load via MainViewModel.NormalizeModel
        // (it's internal, so InternalsVisibleTo would be required for direct access;
        // verify behavior end-to-end via LoadFile instead).
        var dlg = new FakeDialogs();
        var vm = new MainViewModel(dlg);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ady");
        File.WriteAllText(path, JsonConvert.SerializeObject(app));
        try
        {
            vm.LoadFile(path);
            Assert.NotNull(vm.AudysseyMultEQApp);
            Assert.NotNull(vm.AudysseyMultEQApp.DetectedChannels);
            Assert.Single(vm.AudysseyMultEQApp.DetectedChannels);
            Assert.NotNull(vm.AudysseyMultEQApp.DetectedChannels[0].ResponseData);
        }
        finally { File.Delete(path); }
    }
}
