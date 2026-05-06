using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Audyssey;
using Audyssey.MultEQApp;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Ratbuddyssey;

public partial class RatbuddysseyHome : Window, IDialogService
{
    private static readonly FilePickerFileType[] AdyFileTypes =
    {
        new("Audyssey files") { Patterns = new[] { "*.ady" } },
    };

    private static readonly FilePickerFileType[] AdySaveFileTypes =
    {
        new("Audyssey calibration") { Patterns = new[] { "*.ady" } },
    };

    private readonly MainViewModel _viewModel;
    private readonly AudysseyMultEQReferenceCurveFilter audysseyMultEQReferenceCurveFilter = new();

    public RatbuddysseyHome()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        _viewModel = new MainViewModel(this);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = _viewModel;

        BuildFilterSliders();
        AddHandler(DragDrop.DropEvent, HandleDroppedFile);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.AudysseyMultEQApp))
        {
            // Re-render the chart when a new .ady is loaded.
            DrawChart();
        }
    }

    private void HandleDroppedFile(object sender, DragEventArgs e)
    {
        try
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files == null) return;
            foreach (var f in files)
            {
                var path = f.TryGetLocalPath();
                if (string.IsNullOrEmpty(path)) continue;
                if (!string.Equals(Path.GetExtension(path), ".ady", StringComparison.OrdinalIgnoreCase))
                    continue;
                _viewModel.LoadFile(path);
                break;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("HandleDroppedFile failed: {0}", ex);
        }
    }

    // --- IDialogService -------------------------------------------------------

    async Task<string> IDialogService.OpenAdyFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Audyssey file",
            AllowMultiple = false,
            FileTypeFilter = AdyFileTypes,
        });
        return files != null && files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    async Task<string> IDialogService.SaveAdyFileAsAsync(string suggestedName)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            SuggestedFileName = suggestedName,
            DefaultExtension = "ady",
            FileTypeChoices = AdySaveFileTypes,
        });
        return file?.TryGetLocalPath();
    }

    Task<bool> IDialogService.ConfirmReloadAsync() => MessageBoxHelper.ShowYesNoAsync(this,
        "Are you sure?",
        "This will reload the .ady file and discard all changes since last save");

    Task IDialogService.ShowAboutAsync() => MessageBoxHelper.ShowAsync(this,
        "About Ratbuddyssey",
        "Shout out to AVS Forum, use at your own risk!");

    Task IDialogService.ShowErrorAsync(string title, string message)
        => MessageBoxHelper.ShowAsync(this, title, message);

    void IDialogService.RequestExit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            Close();
        }
    }
}

internal static class MessageBoxHelper
{
    public static Task ShowAsync(Window owner, string title, string message)
        => ShowDialogAsync(owner, title, message, false);

    public static Task<bool> ShowYesNoAsync(Window owner, string title, string message)
        => ShowDialogAsync(owner, title, message, true);

    private static Task<bool> ShowDialogAsync(Window owner, string title, string message, bool yesNo)
    {
        var dlg = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var ok = new Button { Content = yesNo ? "Yes" : "OK", IsDefault = true, MinWidth = 80, Margin = new Thickness(4) };
        var cancel = new Button { Content = "No", IsCancel = true, MinWidth = 80, Margin = new Thickness(4) };
        var tcs = new TaskCompletionSource<bool>();
        ok.Click += (_, __) => { tcs.TrySetResult(true); dlg.Close(); };
        cancel.Click += (_, __) => { tcs.TrySetResult(false); dlg.Close(); };

        var btnPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(8),
        };
        btnPanel.Children.Add(ok);
        if (yesNo) btnPanel.Children.Add(cancel);

        var root = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(btnPanel, Dock.Bottom);
        root.Children.Add(btnPanel);
        root.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        dlg.Content = root;
        dlg.Closed += (_, __) => tcs.TrySetResult(false);

        _ = dlg.ShowDialog(owner);
        return tcs.Task;
    }
}
