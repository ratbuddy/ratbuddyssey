using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Audyssey;
using Audyssey.MultEQApp;

namespace Ratbuddyssey
{
    public partial class RatbuddysseyHome : Window
    {
        private AudysseyMultEQReferenceCurveFilter audysseyMultEQReferenceCurveFilter = new AudysseyMultEQReferenceCurveFilter();
        private AudysseyMultEQApp audysseyMultEQApp;

        // Refuse to deserialize anything wildly larger than typical Audyssey calibrations
        // (real-world files are <2 MiB). Defends against zip-bomb-style JSON nesting and
        // accidental drops of huge unrelated files.
        private const long MaxAdyFileBytes = 32L * 1024 * 1024;

        // Pinned defensively even though TypeNameHandling defaults to None.
        private static readonly FilePickerFileType[] AdyFileTypes =
        {
            new("Audyssey files") { Patterns = new[] { "*.ady" } },
        };

        private static readonly FilePickerFileType[] AdySaveFileTypes =
        {
            new("Audyssey calibration") { Patterns = new[] { "*.ady" } },
        };

        private static readonly JsonSerializerSettings AdyReadSettings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal,
            TypeNameHandling = TypeNameHandling.None,
            MaxDepth = 64
        };

        private static readonly JsonSerializerSettings AdyWriteSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None
        };

        public RatbuddysseyHome()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            BuildFilterSliders();
            AddHandler(DragDrop.DropEvent, HandleDroppedFile);
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void HandleDroppedFile(object sender, DragEventArgs e)
        {
            try
            {
                // Avalonia 11.3 deprecated e.Data / DataFormats.Files in favor of the new
                // DataTransfer/DataFormat.File API; the legacy surface still works and the
                // migration is queued behind the broader Avalonia upgrade work.
#pragma warning disable CS0618
                if (e.Data.Contains(DataFormats.Files))
                {
                    var files = e.Data.GetFiles();
#pragma warning restore CS0618
                    if (files == null) return;
                    foreach (var f in files)
                    {
                        var path = f.TryGetLocalPath();
                        if (string.IsNullOrEmpty(path)) continue;
                        if (!string.Equals(Path.GetExtension(path), ".ady", StringComparison.OrdinalIgnoreCase))
                            continue;
                        OpenFile(path);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("HandleDroppedFile failed: {0}", ex);
            }
        }

        private void ParseFileToAudysseyMultEQApp(string fileName)
        {
            if (!File.Exists(fileName)) return;
            var info = new FileInfo(fileName);
            if (info.Length == 0 || info.Length > MaxAdyFileBytes)
            {
                Trace.TraceWarning("Refusing to load '{0}': size {1} outside accepted range (1..{2}).",
                    fileName, info.Length, MaxAdyFileBytes);
                return;
            }

            string serialized = File.ReadAllText(fileName);
            try
            {
                audysseyMultEQApp = JsonConvert.DeserializeObject<AudysseyMultEQApp>(serialized, AdyReadSettings);
            }
            catch (JsonException ex)
            {
                Trace.TraceWarning("Failed to parse '{0}' as Audyssey calibration: {1}", fileName, ex.Message);
                audysseyMultEQApp = null;
            }
        }

        private void ParseAudysseyMultEQAppToFile(string fileName)
        {
            if (audysseyMultEQApp != null)
            {
                string serialized = JsonConvert.SerializeObject(audysseyMultEQApp, AdyWriteSettings);
                if (!string.IsNullOrEmpty(serialized) && !string.IsNullOrEmpty(fileName))
                {
                    File.WriteAllText(fileName, serialized);
                }
            }
        }

        private async void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open Audyssey file",
                    AllowMultiple = false,
                    FileTypeFilter = AdyFileTypes,
                });
                if (files != null && files.Count > 0)
                {
                    var path = files[0].TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path)) OpenFile(path);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("OpenFile_OnClick failed: {0}", ex);
            }
        }

        private async void ReloadFile_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                bool yes = await MessageBoxHelper.ShowYesNoAsync(this,
                    "Are you sure?",
                    "This will reload the .ady file and discard all changes since last save");
                if (!yes) return;
                string path = currentFile.Text;
                if (File.Exists(path))
                {
                    ParseFileToAudysseyMultEQApp(path);
                    if (audysseyMultEQApp != null)
                    {
                        DataContext = audysseyMultEQApp;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("ReloadFile_OnClick failed: {0}", ex);
            }
        }

        private void SaveFile_OnClick(object sender, RoutedEventArgs e)
        {
            string path = currentFile.Text;
#if DEBUG
            if (!string.IsNullOrEmpty(path))
            {
                path = Path.ChangeExtension(path, ".json");
                currentFile.Text = path;
            }
#endif
            ParseAudysseyMultEQAppToFile(path);
        }

        private async void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save As",
                    SuggestedFileName = currentFile.Text,
                    DefaultExtension = "ady",
                    FileTypeChoices = AdySaveFileTypes,
                });
                if (file != null)
                {
                    var path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        currentFile.Text = path;
                        ParseAudysseyMultEQAppToFile(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("SaveFileAs_OnClick failed: {0}", ex);
            }
        }

        private void ExitProgram_OnClick(object sender, RoutedEventArgs e)
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

        private async void About_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await MessageBoxHelper.ShowAsync(this, "About Ratbuddyssey",
                    "Shout out to AVS Forum, use at your own risk!");
            }
            catch (Exception ex)
            {
                Trace.TraceError("About_OnClick failed: {0}", ex);
            }
        }

        private void OpenFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                currentFile.Text = filePath;
                ParseFileToAudysseyMultEQApp(filePath);
                if (audysseyMultEQApp != null)
                {
                    DataContext = audysseyMultEQApp;
                }
            }
        }
    }

    internal static class MessageBoxHelper
    {
        public static Task ShowAsync(Window owner, string title, string message)
            => ShowDialogAsync(owner, title, message, false);

        public static async Task<bool> ShowYesNoAsync(Window owner, string title, string message)
            => await ShowDialogAsync(owner, title, message, true);

        private static Task<bool> ShowDialogAsync(Window owner, string title, string message, bool yesNo)
        {
            var dlg = new Window
            {
                Title = title,
                Width = 420,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var ok = new Button { Content = yesNo ? "Yes" : "OK", IsDefault = true, MinWidth = 80, Margin = new Avalonia.Thickness(4) };
            var cancel = new Button { Content = "No", IsCancel = true, MinWidth = 80, Margin = new Avalonia.Thickness(4) };
            var tcs = new TaskCompletionSource<bool>();
            ok.Click += (_, __) => { tcs.TrySetResult(true); dlg.Close(); };
            cancel.Click += (_, __) => { tcs.TrySetResult(false); dlg.Close(); };

            var btnPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(8)
            };
            btnPanel.Children.Add(ok);
            if (yesNo) btnPanel.Children.Add(cancel);

            var root = new DockPanel { Margin = new Avalonia.Thickness(12) };
            DockPanel.SetDock(btnPanel, Dock.Bottom);
            root.Children.Add(btnPanel);
            root.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            dlg.Content = root;
            dlg.Closed += (_, __) => tcs.TrySetResult(false);

            _ = dlg.ShowDialog(owner);
            return tcs.Task;
        }
    }
}
