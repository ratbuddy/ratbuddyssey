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
    private readonly UserSettingsService _settings = new();
    private readonly AudysseyMultEQReferenceCurveFilter audysseyMultEQReferenceCurveFilter = new();

    public RatbuddysseyHome()
    {
        // Generator-emitted InitializeComponent(bool, bool) loads XAML, wires
        // every x:Name field, and attaches DevTools under DEBUG.
        InitializeComponent();
        _viewModel = new MainViewModel(this);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = _viewModel;

        // Restore the user's last theme choice before any chart is drawn.
        ThemeService.Current = ThemeService.Parse(_settings.Current.Theme);
        RebuildRecentFilesMenu();

        BuildFilterSliders();
        AddHandler(DragDrop.DropEvent, HandleDroppedFile);

        ApplyPlotTheme();
        ActualThemeVariantChanged += (_, __) =>
        {
            ApplyPlotTheme();
            DrawChart();
        };

        Closing += OnWindowClosing;
        Opened += OnWindowOpened;
    }

    private bool _exitConfirmed;

    private async void OnWindowClosing(object sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (_exitConfirmed || !_viewModel.IsDirty) return;
        e.Cancel = true;
        try
        {
            bool ok = await ((IDialogService)this).ConfirmDiscardChangesAsync();
            if (ok)
            {
                _exitConfirmed = true;
                Close();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("OnWindowClosing failed: {0}", ex);
        }
    }

    private void OnWindowOpened(object sender, EventArgs e)
    {
        // Honor a single .ady path on the command line so the OS file
        // association (or `ratbuddyssey foo.ady`) opens it on launch.
        try
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                string candidate = args[1];
                if (!string.IsNullOrWhiteSpace(candidate)
                    && System.IO.File.Exists(candidate)
                    && string.Equals(System.IO.Path.GetExtension(candidate), ".ady", StringComparison.OrdinalIgnoreCase))
                {
                    _viewModel.LoadFile(candidate);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("OnWindowOpened failed: {0}", ex);
        }
    }

    private void OnLightThemeClicked(object sender, RoutedEventArgs e) => SetTheme("Light");
    private void OnDarkThemeClicked(object sender, RoutedEventArgs e) => SetTheme("Dark");
    private void OnSystemThemeClicked(object sender, RoutedEventArgs e) => SetTheme("System");

    private async void OnApplyAudysseyOnePostProcessClicked(object sender, RoutedEventArgs e)
    {
        var app = _viewModel.AudysseyMultEQApp;
        if (app == null)
        {
            await MessageBoxHelper.ShowAsync(this, "Apply AudysseyOne post-process",
                "Open an .ady file first.");
            return;
        }

        bool ok = await MessageBoxHelper.ShowYesNoAsync(this,
            "Apply AudysseyOne post-process?",
            "This is destructive: it overwrites every measurement response with a "
            + "neutral impulse, forces DynamicEQ / DynamicVolume / Lfc off, "
            + "and snaps per-channel rolloff/level/distance to safe defaults. "
            + "Save your file first if you want to keep the originals.\n\n"
            + "Continue?");
        if (!ok) return;

        AudysseyOnePostProcess.Apply(app);
        _viewModel.IsDirty = true;
        DrawChart();
    }

    private async void OnApplyHouseCurveToSelectedClicked(object sender, RoutedEventArgs e)
    {
        var curve = _viewModel.SelectedHouseCurve;
        var ch = SelectedDetectedChannel();
        if (curve == null) return;
        if (ch == null)
        {
            await MessageBoxHelper.ShowAsync(this, "Apply house curve",
                "Select a channel in the list above first.");
            return;
        }
        HouseCurves.ApplyTo(ch, curve);
        _viewModel.IsDirty = true;
        DrawChart();
    }

    private async void OnApplyHouseCurveToAllClicked(object sender, RoutedEventArgs e)
    {
        var curve = _viewModel.SelectedHouseCurve;
        var app = _viewModel.AudysseyMultEQApp;
        if (curve == null) return;
        if (app?.DetectedChannels == null || app.DetectedChannels.Count == 0)
        {
            await MessageBoxHelper.ShowAsync(this, "Apply house curve",
                "Open an .ady file first.");
            return;
        }
        bool ok = await MessageBoxHelper.ShowYesNoAsync(this,
            "Apply house curve to all channels?",
            $"This will overwrite the existing target curve points on every "
            + $"channel with the '{curve.Name}' preset. Subwoofers receive only "
            + $"the bass-shelf portion (trimmed at 200 Hz). Continue?");
        if (!ok) return;
        int n = HouseCurves.ApplyToAll(app.DetectedChannels, curve);
        _viewModel.IsDirty = true;
        DrawChart();
        Trace.TraceInformation("Applied house curve '{0}' to {1} channel(s).", curve.Name, n);
    }

    private void SetTheme(string name)
    {
        ThemeService.Current = ThemeService.Parse(name);
        _settings.Current.Theme = name;
        _settings.Save();
    }

    private void RebuildRecentFilesMenu()
    {
        var menu = this.FindControl<MenuItem>("RecentFilesMenu");
        if (menu == null) return;
        menu.Items.Clear();
        var list = _settings.Current.RecentFiles;
        if (list == null || list.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "(empty)", IsEnabled = false });
            menu.IsEnabled = true;
            return;
        }
        for (int i = 0; i < list.Count; i++)
        {
            string path = list[i];
            var item = new MenuItem { Header = $"_{i + 1}  {path}" };
            item.Click += (_, __) =>
            {
                if (System.IO.File.Exists(path))
                {
                    _viewModel.LoadFile(path);
                    _settings.AddRecentFile(path);
                    RebuildRecentFilesMenu();
                }
                else
                {
                    _settings.RemoveRecentFile(path);
                    RebuildRecentFilesMenu();
                }
            };
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var clear = new MenuItem { Header = "_Clear recent files" };
        clear.Click += (_, __) =>
        {
            _settings.Current.RecentFiles?.Clear();
            _settings.Save();
            RebuildRecentFilesMenu();
        };
        menu.Items.Add(clear);
    }

    /// <summary>
    /// Pushes Avalonia theme colors into ScottPlot so the chart matches light / dark mode.
    /// Called on startup and whenever <see cref="StyledElement.ActualThemeVariantChanged"/> fires.
    /// </summary>
    private void ApplyPlotTheme()
    {
        if (plot == null) return;

        bool dark = ThemeService.IsDark;
        var fig = dark ? new ScottPlot.Color(30, 30, 30) : ScottPlot.Colors.White;
        var data = dark ? new ScottPlot.Color(40, 40, 40) : ScottPlot.Colors.White;
        var axis = dark ? new ScottPlot.Color(220, 220, 220) : new ScottPlot.Color(40, 40, 40);
        var grid = dark ? new ScottPlot.Color(70, 70, 70) : new ScottPlot.Color(220, 220, 220);

        var p = plot.Plot;
        p.FigureBackground.Color = fig;
        p.DataBackground.Color = data;
        foreach (var ax in p.Axes.GetAxes())
        {
            ax.FrameLineStyle.Color = axis;
            ax.MajorTickStyle.Color = axis;
            ax.MinorTickStyle.Color = axis;
            ax.TickLabelStyle.ForeColor = axis;
            ax.Label.ForeColor = axis;
        }
        p.Axes.Title.Label.ForeColor = axis;
        p.Grid.MajorLineColor = grid;
        p.Grid.MinorLineColor = grid;

        // The first measurement slot uses pure black for max contrast in light
        // mode; that's invisible against the dark figure background, so swap
        // to near-white when the dark theme is active.
        AdaptFirstMeasurementSlotToTheme(dark);

        plot.Refresh();
    }

    // NOTE: Do NOT add a private parameterless InitializeComponent() here.
    // The Avalonia name-source generator emits a public
    // InitializeComponent(bool, bool) that both loads XAML and binds every
    // x:Name field (plot, channelsView, chbxLogarithmicAxis, ...). A private
    // overload would shadow it for the constructor's InitializeComponent()
    // call, leaving every named control null at runtime.

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.AudysseyMultEQApp))
        {
            // Re-render the chart when a new .ady is loaded.
            DrawChart();
        }
        else if (e.PropertyName == nameof(MainViewModel.CurrentFilePath))
        {
            string path = _viewModel.CurrentFilePath;
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                _settings.AddRecentFile(path);
                RebuildRecentFilesMenu();
            }
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

    Task<bool> IDialogService.ConfirmDiscardChangesAsync() => MessageBoxHelper.ShowYesNoAsync(this,
        "Discard unsaved changes?",
        "You have unsaved changes. Continue and discard them?");

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
