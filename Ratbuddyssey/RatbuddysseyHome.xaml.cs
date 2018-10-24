using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Ratbuddyssey
{
    /// <summary>
    /// Interaction logic for RatbuddysseyHome.xaml
    /// </summary>
    public partial class RatbuddysseyHome : Page
    {
        Audyssey parsedAudyssey = null;
        private string filename;
        private string trimmedFilename;
        PlotModel plotModel = new PlotModel();
        ObservableCollection<DataPoint> points = new ObservableCollection<DataPoint>();

        public RatbuddysseyHome()
        {
            InitializeComponent();
            responseCombo.SelectionChanged += ResponseCombo_SelectionChanged;
            channelsView.SelectionChanged += ChannelsView_SelectionChanged;
            plot.PreviewMouseWheel += Plot_PreviewMouseWheel;
        }

        private void Plot_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void ChannelsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DetectedChannel selectedChannel = (DetectedChannel)channelsView.SelectedValue;
            if (selectedChannel.ResponseData.Count > 0)
            {
                responseCombo.SelectedIndex = 0;
            }
        }

        private void ResponseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (plot.Model != null && plot.Model.Series != null)
            {
                plot.Model.Series.Clear();
                plot.Model = null;
            }

            DetectedChannel selectedChannel = (DetectedChannel)channelsView.SelectedValue;
            if (responseCombo.SelectedValue != null)
            {
                string s = ((KeyValuePair<string, string[]>)responseCombo.SelectedValue).Key;
                string[] values = selectedChannel.ResponseData[s];
                int count = values.Length;
                Complex[] cValues = new Complex[count];
                double[] Xs = new double[count];
                float sample_rate = 48000;
                float total_time = count / sample_rate;
                for (int i = 0; i < count; i++)
                {
                    decimal d = Decimal.Parse(values[i], NumberStyles.AllowExponent | NumberStyles.Float);
                    Complex cValue = (Complex)d;
                    cValues[i] = cValue;
                    Xs[i] = (double)i / count * sample_rate/1000; // units are in kHz
                }

                Complex[] result = FFT.fft(cValues);
                int x = 0;
                points.Clear();
                foreach (Complex cValue in result)
                {
                    //Add data point here
                    points.Add(new DataPoint(Xs[x], 20 * Math.Log10(cValue.Magnitude)));
                    x++;
                    if (x == count / 2) break;
                }
                LineSeries lineserie = new LineSeries
                {
                    ItemsSource = points,
                    DataFieldX = "x",
                    DataFieldY = "Y",
                    StrokeThickness = 1,
                    MarkerSize = 0,
                    LineStyle = LineStyle.Solid,
                    Color = OxyColors.Red,
                    Title = "Frequency",
                    MarkerType = MarkerType.None,
                };
                plotModel.Series.Add(lineserie);
                plotModel.Axes.Clear();
                if (chbxLogX.IsChecked.Value)
                {
                    plotModel.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Title = "kHz", AbsoluteMinimum=0.01 });
                }
                else
                {
                    plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "kHz" });
                }
                plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "dB" });
                plot.Model = plotModel;
            }
        }

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey files (*.ady)|*.ady";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                currentFile.Content = filename;
                String audysseyFile = File.ReadAllText(filename);
                parsedAudyssey = JsonConvert.DeserializeObject<Audyssey>(audysseyFile, new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                });
                if(parsedAudyssey!=null)
                {
                    this.DataContext = parsedAudyssey;
                }

            }
        }

        private void ReloadFile_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will reload the .ady file and discard all changes since last save", "Are you sure?", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                String audysseyFile = File.ReadAllText(filename);
                parsedAudyssey = JsonConvert.DeserializeObject<Audyssey>(audysseyFile, new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                });
                if (parsedAudyssey != null)
                {
                    this.DataContext = parsedAudyssey;
                }
            }
        }

        private void SaveFile_OnClick(object sender, RoutedEventArgs e)
        {
            string reSerialized = JsonConvert.SerializeObject(parsedAudyssey, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
//            trimmedFilename = (filename.Substring(0, filename.Length - 3));
//            File.WriteAllText(trimmedFilename + "modified_by_ratbuddyssey.ady", reSerialized);

            File.WriteAllText(filename, reSerialized);
        }

        private void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            trimmedFilename = (filename.Substring(0, filename.Length - 3));
            dlg.FileName = filename;
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey calibration (.ady)|*.ady";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                filename = dlg.FileName;
                string reSerialized = JsonConvert.SerializeObject(parsedAudyssey, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

                File.WriteAllText(filename, reSerialized);

            }
        }

        private void ExitProgram_OnClick(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Close();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Shout out to AVS Forum, use at your own risk!");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(keyTbx.Text) && !string.IsNullOrEmpty(valueTbx.Text))
            {
                ((DetectedChannel)channelsView.SelectedValue).CustomTargetCurvePointsDictionary.Add(new MyKeyValuePair(keyTbx.Text, valueTbx.Text));
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            MyKeyValuePair pair = b.DataContext as MyKeyValuePair;
            ((DetectedChannel)channelsView.SelectedValue).CustomTargetCurvePointsDictionary.Remove(pair);            
        }
    }
}
