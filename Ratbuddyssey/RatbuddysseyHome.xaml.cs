using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
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
        private Audyssey parsedAudyssey = null;

        private string filename;
        private PlotModel plotModel = new PlotModel();
        
        private List<int> keys = new List<int>();
        private Dictionary<int, Brush> colors = new Dictionary<int, Brush>();
        
        private double smoothingFactor = 0;
        
        private DetectedChannel selectedChannel = null;
        private List<DetectedChannel> stickyChannel = new List<DetectedChannel>();

        private string selectedAxisLimits = "rbtnXRangeFull";
        private Dictionary<string, AxisLimit> AxisLimits = new Dictionary<string, AxisLimit>()
        {
            {"rbtnXRangeFull", new AxisLimit { XMin = 10, XMax = 24000, YMin = -35, YMax = 20, YShift = 0, MajorStep = 5, MinorStep = 1 } },
            {"rbtnXRangeSubwoofer", new AxisLimit { XMin = 10, XMax = 1000, YMin = -35, YMax = 20, YShift = 0, MajorStep = 5, MinorStep = 1 } },
            {"rbtnXRangeChirp", new AxisLimit { XMin = 0, XMax = 350, YMin = -0.1, YMax = 0.1, YShift = 0, MajorStep = 0.01, MinorStep = 0.001 } }
        };

        private ReferenceCurveFilter referenceCurveFilter = new ReferenceCurveFilter();

        private AvrAudysseyAdapter AvrAudysseyAdapter = null;
        public RatbuddysseyHome()
        {
            InitializeComponent();
            channelsView.SelectionChanged += ChannelsView_SelectionChanged;
            plot.PreviewMouseWheel += Plot_PreviewMouseWheel;
        }
        ~RatbuddysseyHome()
        {
        }
        private void DrawChart()
        {
            if (plot != null)
            {
                ClearPlot();
                if (selectedChannel != null)
                {
                    PlotLine(selectedChannel);
                }
                if (stickyChannel != null)
                {
                    foreach(var channel in stickyChannel)
                    {
                        if (channel.Sticky == true)
                        {
                            PlotLine(channel, true);
                        }
                    }
                }
                switch(TargetCurveType.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        PlotLine(null, false);
                        break;
                    case 2:
                        PlotLine(null, true);
                        break;
                    default:
                        PlotLine(null, false);
                        PlotLine(null, true);
                        break;

                }
                PlotAxis();
                PlotChart();
            }
        }
        private void ClearPlot()
        {
            if (plot.Model != null && plot.Model.Series != null)
            {
                plot.Model.Series.Clear();
                plot.Model = null;
            }
        }
        private void PlotChart()
        {
            plot.Model = plotModel;
        }
        private void PlotAxis()
        {
            plotModel.Axes.Clear();
            AxisLimit Limits = AxisLimits[selectedAxisLimits];
            if (selectedAxisLimits == "rbtnXRangeChirp")
            {
                if (chbxLogarithmicAxis != null)
                {
                    if (chbxLogarithmicAxis.IsChecked == true)
                    {
                        plotModel.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Title = "ms", Minimum = Limits.XMin, Maximum = Limits.XMax, MajorGridlineStyle = LineStyle.Dot });
                    }
                    else
                    {
                        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "ms", Minimum = Limits.XMin, Maximum = Limits.XMax, MajorGridlineStyle = LineStyle.Dot });
                    }
                }
                plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "", Minimum = Limits.YMin, Maximum = Limits.YMax, MajorStep = Limits.MajorStep, MinorStep = Limits.MinorStep, MajorGridlineStyle = LineStyle.Solid });
            }
            else
            {
                if (chbxLogarithmicAxis != null)
                {
                    if (chbxLogarithmicAxis.IsChecked == true)
                    {
                        plotModel.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Title = "Hz", Minimum = Limits.XMin, Maximum = Limits.XMax, MajorGridlineStyle = LineStyle.Dot });
                    }
                    else
                    {
                        plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Hz", Minimum = Limits.XMin, Maximum = Limits.XMax, MajorGridlineStyle = LineStyle.Dot });
                    }
                }
                plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "dB", Minimum = Limits.YMin + Limits.YShift, Maximum = Limits.YMax + Limits.YShift, MajorStep = Limits.MajorStep, MinorStep = Limits.MinorStep, MajorGridlineStyle = LineStyle.Solid });
            }
        }
        private void PlotLine(DetectedChannel selectedChannel, bool secondaryChannel = false)
        {
            if (selectedChannel == null)
            {
                Collection<DataPoint> points = null;
                //time domain data
                if (selectedAxisLimits == "rbtnXRangeChirp")
                {
                }
                //frequency domain data
                else
                {
                    if (secondaryChannel)
                    {
                        points = referenceCurveFilter.High_Frequency_Roll_Off_2();
                    }
                    else
                    {
                        points = referenceCurveFilter.High_Frequency_Roll_Off_1();
                    }

                    if (points != null)
                    {
                        OxyColor color = OxyColor.FromRgb(255, 0, 0);
                        LineSeries lineserie = new LineSeries
                        {
                            ItemsSource = points,
                            DataFieldX = "X",
                            DataFieldY = "Y",
                            StrokeThickness = 2,
                            MarkerSize = 0,
                            LineStyle = LineStyle.Solid,
                            Color = color,
                            MarkerType = MarkerType.None,
                        };
                        plotModel.Series.Add(lineserie);
                    }
                }
            }
            else
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    Collection<DataPoint> points = new Collection<DataPoint>();

                    string s = keys[i].ToString();
                    string[] values = selectedChannel.ResponseData[s];
                    int count = values.Length;
                    Complex[] cValues = new Complex[count];
                    double[] Xs = new double[count];

                    float sample_rate = 48000;
                    float total_time = count / sample_rate;

                    AxisLimit Limits = AxisLimits[selectedAxisLimits];
                    if (selectedAxisLimits == "rbtnXRangeChirp")
                    {
                        Limits.XMax = 1000 * total_time; // horizotal scale: s to ms
                        for (int j = 0; j < count; j++)
                        {
                            double d = Double.Parse(values[j], NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture);
                            points.Add(new DataPoint(1000 * j * total_time / count, d));
                        }
                    }
                    else
                    {
                        for (int j = 0; j < count; j++)
                        {
                            decimal d = Decimal.Parse(values[j], NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture);
                            Complex cValue = (Complex)d;
                            cValues[j] = 100 * cValue;
                            Xs[j] = (double)j / count * sample_rate;
                        }

                        MathNet.Numerics.IntegralTransforms.Fourier.Forward(cValues);

                        int x = 0;
                        if (rbtnNo.IsChecked.Value)
                        {
                            foreach (Complex cValue in cValues)
                            {
                                points.Add(new DataPoint(Xs[x++], Limits.YShift + 20 * Math.Log10(cValue.Magnitude)));
                                if (x == count / 2) break;
                            }
                        }
                        else
                        {
                            double[] smoothed = new double[count];
                            for (int j = 0; j < count; j++)
                            {
                                smoothed[j] = cValues[j].Magnitude;
                            }

                            LinSpacedFracOctaveSmooth(smoothingFactor, ref smoothed, 1, 1d / 48);

                            foreach (double smoothetResult in smoothed)
                            {
                                points.Add(new DataPoint(Xs[x++], Limits.YShift + 20 * Math.Log10(smoothetResult)));
                                if (x == count / 2) break;
                            }
                        }
                    }

                    OxyColor color = OxyColor.Parse(colors[keys[i]].ToString());
                    LineSeries lineserie = new LineSeries
                    {
                        ItemsSource = points,
                        DataFieldX = "X",
                        DataFieldY = "Y",
                        StrokeThickness = 1,
                        MarkerSize = 0,
                        LineStyle = secondaryChannel ? LineStyle.Dot : LineStyle.Solid,
                        Color = color,
                        MarkerType = MarkerType.None,
                    };

                    plotModel.Series.Add(lineserie);
                }
            }
        }
        private void LinSpacedFracOctaveSmooth(double frac, ref double[] smoothed, float startFreq, double freqStep)
        {
            int passes = 8;
            // Scale octave frac to allow for number of passes
            double scaledFrac = 7.5 * frac; //Empirical tweak to better match Gaussian smoothing
            double octMult = Math.Pow(2, 0.5 / scaledFrac);
            double bwFactor = (octMult - 1 / octMult);
            double b = 0.5 + bwFactor * startFreq / freqStep;
            int N = smoothed.Length;
            double xp;
            double yp;
            // Smooth from HF to LF to avoid artificial elevation of HF data
            for (int pass = 0; pass < passes; pass++)
            {
                xp = smoothed[N - 1];
                yp = xp;
                // reverse pass
                for (int i = N - 2; i >= 0; i--)
                {
                    double a = 1 / (b + i * bwFactor);
                    yp += ((xp + smoothed[i]) / 2 - yp) * a;
                    xp = smoothed[i];
                    smoothed[i] = (float)yp;
                }
                // forward pass
                for (int i = 1; i < N; i++)
                {
                    double a = 1 / (b + i * bwFactor);
                    yp += ((xp + smoothed[i]) / 2 - yp) * a;
                    xp = smoothed[i];
                    smoothed[i] = (float)yp;
                }
            }
        }
        private void Plot_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
        private void Chbx_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBoc = sender as CheckBox;
            int val = int.Parse(checkBoc.Content.ToString()) - 1;
            if (keys.Contains(val))
            {
                keys.Remove(val);
                colors.Remove(val);
            }
            DrawChart();
        }
        private void Chbx_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            int val = int.Parse(checkBox.Content.ToString()) - 1;
            if (!keys.Contains(val))
            {
                keys.Add(val);
                colors.Add(val, checkBox.Foreground);
            }
            DrawChart();
        }
        private void ChannelsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((DetectedChannel)channelsView.SelectedValue != null) && (((DetectedChannel)channelsView.SelectedValue).ResponseData != null))
            {
                if (((DetectedChannel)channelsView.SelectedValue).ResponseData.Count > 0)
                {
                    selectedChannel = (DetectedChannel)channelsView.SelectedValue;
                    DrawChart();
                }
            }
        }
        private void ChannelsView_OnClickSticky(object sender, RoutedEventArgs e)
        {
            foreach (var channel in parsedAudyssey.DetectedChannels)
            {
                if (channel.Sticky)
                {
                    stickyChannel.Add(channel);
                    DrawChart();
                }
                else if (stickyChannel.Contains(channel))
                {
                    stickyChannel.Remove(channel);
                    DrawChart();
                }
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
                // Load document 
                String audysseyFile = File.ReadAllText(filename);
                // Parse JSON data
                parsedAudyssey = JsonConvert.DeserializeObject<Audyssey>(audysseyFile,
                    new JsonSerializerSettings{FloatParseHandling = FloatParseHandling.Decimal});
                // Data Binding
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
                // Reload document 
                String audysseyFile = File.ReadAllText(filename);
                // Parse JSON data
                parsedAudyssey = JsonConvert.DeserializeObject<Audyssey>(audysseyFile,
                    new JsonSerializerSettings{FloatParseHandling = FloatParseHandling.Decimal});
                // Data Binding
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
#if DEBUG
            filename = System.IO.Path.ChangeExtension(filename, ".json");
#endif
            if ((reSerialized != null) && (!string.IsNullOrEmpty(filename)))
            {
                File.WriteAllText(filename, reSerialized);
            }
        }
        private void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
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
                if ((reSerialized != null) && (!string.IsNullOrEmpty(filename)))
                {
                    File.WriteAllText(filename, reSerialized);
                }
            }
        }
        private void connectEthernet_Click(object sender, RoutedEventArgs e)
        {
            if (sender == connectEthernet)
            {
                if(connectEthernet.IsChecked)
                {
                    // Establish ethernet connection with receiver
                    AvrAudysseyAdapter = new AvrAudysseyAdapter(connectSniffer.IsChecked);
                    if (AvrAudysseyAdapter != null)
                    {
                        // Data Binding
                        this.DataContext = AvrAudysseyAdapter;
                        // Display connection details
                        if (connectSniffer.IsChecked)
                        {
                            currentFile.Content = "Host: " + AvrAudysseyAdapter.GetTcpHost() + " Client:" + AvrAudysseyAdapter.GetTcpClient();
                        }
                        else
                        {
                            currentFile.Content = "Client:" + AvrAudysseyAdapter.GetTcpClient();
                        }
                    }
                    // Check if binding and propertychanged work
                    AvrAudysseyAdapter.AudysseyToAvr();
                }
                else
                {
                    this.DataContext = null;
                    AvrAudysseyAdapter = null;
                    // immediately clean up the object
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    currentFile.Content = "";
                }
            }
        }
        private void connectSniffer_Click(object sender, RoutedEventArgs e)
        {
            if (sender == connectSniffer)
            {
                if (connectEthernet.IsChecked)
                {
                    if (AvrAudysseyAdapter != null)
                    {
                        AvrAudysseyAdapter.AttachSniffer();
                        currentFile.Content = "Host: " + AvrAudysseyAdapter.GetTcpHost() + " Client:" + AvrAudysseyAdapter.GetTcpClient();
                    }
                }
                else
                {
                    if (AvrAudysseyAdapter != null)
                    {
                        AvrAudysseyAdapter.DetachSniffer();
                        currentFile.Content = "Client:" + AvrAudysseyAdapter.GetTcpClient();
                    }
                }
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
        private void allChbx_Checked(object sender, RoutedEventArgs e)
        {
            chbx1.IsChecked = true;
            chbx2.IsChecked = true;
            chbx3.IsChecked = true;
            chbx4.IsChecked = true;
            chbx5.IsChecked = true;
            chbx6.IsChecked = true;
            chbx7.IsChecked = true;
            chbx8.IsChecked = true;
            DrawChart();
        }
        private void allChbx_Unchecked(object sender, RoutedEventArgs e)
        {
            chbx1.IsChecked = false;
            chbx2.IsChecked = false;
            chbx3.IsChecked = false;
            chbx4.IsChecked = false;
            chbx5.IsChecked = false;
            chbx6.IsChecked = false;
            chbx7.IsChecked = false;
            chbx8.IsChecked = false;
            DrawChart();
        }
        private void rbtn_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            switch (radioButton.Name)
            {
                case "rbtn2":
                    smoothingFactor = 2;
                    break;
                case "rbtn3":
                    smoothingFactor = 3;
                    break;
                case "rbtn6":
                    smoothingFactor = 6;
                    break;
                case "rbtn12":
                    smoothingFactor = 12;
                    break;
                case "rbtn24":
                    smoothingFactor = 24;
                    break;
                case "rbtn48":
                    smoothingFactor = 48;
                    break;
                default:
                    break;
            }
            DrawChart();
        }
        private void rbtnXRange_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            selectedAxisLimits = radioButton.Name;
            DrawChart();
        }
        private void chbxStickSubwoofer_Checked(object sender, RoutedEventArgs e)
        {
            DrawChart();
        }
        private void chbxLogarithmicAxis_Checked(object sender, RoutedEventArgs e)
        {
            DrawChart();
        }
        private void chbxLogarithmicAxis_Unchecked(object sender, RoutedEventArgs e)
        {
            DrawChart();
        }
        private void TargetCurveTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawChart();
        }
    }
    class AxisLimit
    {
        public double XMin { get; set; }
        public double XMax { get; set; }
        public double YMin { get; set; }
        public double YMax { get; set; }
        public double YShift { get; set; }
        public double MajorStep { get; set; }
        public double MinorStep { get; set; }
    }
}