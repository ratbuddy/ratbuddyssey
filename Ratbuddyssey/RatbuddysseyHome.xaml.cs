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
        
        List<int> keys = new List<int>();
        Dictionary<int, Brush> colors = new Dictionary<int, Brush>();
        double smoothingFactor = 2;
        DetectedChannel selectedChannel = null;
        double YMin = 50;
        double YMax = 100;
        double XMin = 15;
        double XMax = 20000;
        double VerticalShift = 75;
        public RatbuddysseyHome()
        {
            InitializeComponent();
            //responseCombo.SelectionChanged += ResponseCombo_SelectionChanged;
            channelsView.SelectionChanged += ChannelsView_SelectionChanged;
            plot.PreviewMouseWheel += Plot_PreviewMouseWheel;
            chbx1.Checked += Chbx_Checked;
            chbx1.Unchecked += Chbx_Unchecked;
        }

        private void Chbx_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox ch = sender as CheckBox;
            int val = int.Parse(ch.Content.ToString()) - 1;
            if (keys.Contains(val))
            {
                keys.Remove(val);
                colors.Remove(val);
            }
            if (selectedChannel!=null && selectedChannel.ResponseData.Count > 0)
            {
                DrawChart(selectedChannel);
                //responseCombo.SelectedIndex = 0;
            }
        }

        private void Chbx_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox ch = sender as CheckBox;
            int val = int.Parse(ch.Content.ToString()) - 1;
            if (!keys.Contains(val))
            {
                keys.Add(val);
                colors.Add(val, ch.Foreground);
            }
            selectedChannel = (DetectedChannel)channelsView.SelectedValue;
            if (selectedChannel != null && selectedChannel.ResponseData.Count > 0)
            {
                DrawChart(selectedChannel);
                //responseCombo.SelectedIndex = 0;
            }
        }

        private void Plot_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void ChannelsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedChannel = (DetectedChannel)channelsView.SelectedValue;
            if (selectedChannel != null)
            {
                if (selectedChannel.EnChannelType == 54 || selectedChannel.EnChannelType == 42)
                {
                    rbtnXRangeSubwoower.IsChecked = true;
                }
                else
                {
                    rbtnXRangeOther.IsChecked = true;
                }
                if (selectedChannel.ResponseData.Count > 0)
                {
                    DrawChart(selectedChannel);
                    //responseCombo.SelectedIndex = 0;
                }
            }
        }

        private void DrawChart(DetectedChannel selectedChannel)
        {
            if (plot.Model != null && plot.Model.Series != null)
            {
                plot.Model.Series.Clear();
                plot.Model = null;
            }
            for (int i=0; i<keys.Count; i++)
            {
                ObservableCollection<DataPoint> points = new ObservableCollection<DataPoint>();
                string s = keys[i].ToString();
                string[] values = selectedChannel.ResponseData[s];
                int count = values.Length;
                Complex[] cValues = new Complex[count];
                double[] Xs = new double[count];
                float sample_rate = 48000;
                float total_time = count / sample_rate;
                for (int j = 0; j < count; j++)
                {
                    decimal d = Decimal.Parse(values[j], NumberStyles.AllowExponent | NumberStyles.Float);
                    Complex cValue = (Complex)d;
                    cValues[j] = cValue;
                    Xs[j] = (double)j / count * sample_rate; // units are in kHz
                }

                Complex[] result = FFT.fft(cValues);
                int x = 0;
                points.Clear();
                double[] smoothed = new double[count];
                for (int j = 0; j < count; j++)
                {
                    smoothed[j] = result[j].Magnitude;
                }
                if(rbtnNo.IsChecked.Value)
                {
                    foreach (Complex cValue in result)
                    {
                        //Add data point here
                        points.Add(new DataPoint(Xs[x], VerticalShift + 20*Math.Log10(cValue.Magnitude)));
                        x++;
                        if (x == count / 2) break;
                    }
                }
                else
                {
                    LinSpacedFracOctaveSmooth(smoothingFactor, ref smoothed, 1, 1d / 48);

                    foreach (double smoothetResult in smoothed)
                    {
                        points.Add(new DataPoint(Xs[x], VerticalShift + 20*Math.Log10(smoothetResult)));
                        x++;
                        if (x == count / 2) break;
                    }
                }
                
                OxyColor color = OxyColor.Parse(colors[keys[i]].ToString());
                LineSeries lineserie = new LineSeries
                {
                    ItemsSource = points,
                    DataFieldX = "x",
                    DataFieldY = "Y",
                    StrokeThickness = 1,
                    MarkerSize = 0,
                    LineStyle = LineStyle.Solid,
                    Color = color,
                    MarkerType = MarkerType.None,
                };
                plotModel.Series.Add(lineserie);
                plotModel.Axes.Clear();                
            }
            if (chbxLogX.IsChecked.Value)
            {
                plotModel.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Title = "Hz", Minimum = XMin, Maximum = XMax, MajorGridlineStyle = LineStyle.Dot });
            }
            else
            {
                plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Hz", Minimum=XMin, Maximum=XMax, MajorGridlineStyle = LineStyle.Dot });
            }
//            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "dB", MajorGridlineStyle = "Dot", MajorGridlineColor = "LightGray", Maximum = VerticalShift+15, Minimum = VerticalShift-25 });
            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "dB", Maximum = VerticalShift + 15, Minimum = VerticalShift - 25, MajorStep = 5, MinorStep = 1, MajorGridlineStyle = LineStyle.Solid});
            plot.Model = plotModel;
        }

        private void LinSpacedFracOctaveSmooth(double frac, ref double[] smoothed, float startFreq, double freqStep)
        {
            int passes = 8;
            // Scale octave frac to allow for number of passes
            double scaledFrac = 7.5*frac; //Empirical tweak to better match Gaussian smoothing
            double octMult = Math.Pow(2, 0.5 / scaledFrac);
            double bwFactor = (octMult - 1 / octMult);
            double b = 0.5 + bwFactor*startFreq / freqStep;
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
                    double a = 1 / (b + i*bwFactor);
                    yp += ((xp + smoothed[i]) / 2 - yp)*a;
                    xp = smoothed[i];
                    smoothed[i] = (float)yp;
                }
                // forward pass
                for (int i = 1; i < N; i++)
                {
                    double a = 1 / (b + i*bwFactor);
                    yp += ((xp + smoothed[i]) / 2 - yp) * a;
                    xp = smoothed[i];
                    smoothed[i] = (float)yp;
                }
            }
        }
        //private void ResponseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (plot.Model != null && plot.Model.Series != null)
        //    {
        //        plot.Model.Series.Clear();
        //        plot.Model = null;
        //    }

        //    DetectedChannel selectedChannel = (DetectedChannel)channelsView.SelectedValue;
        //    if (responseCombo.SelectedValue != null)
        //    {
                
        //    }
        //}

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

        private void rbtn_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rbtn = sender as RadioButton;
            switch (rbtn.Name)
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
                default:
                    break;
            }
            if (selectedChannel != null && selectedChannel.ResponseData.Count > 0)
            {
                DrawChart(selectedChannel);
                //responseCombo.SelectedIndex = 0;
            }
        }

        private void rbtnXRangeSubwoower_Checked(object sender, RoutedEventArgs e)
        {
            XMax = 200;
            XMin = 5;
            selectedChannel = (DetectedChannel)channelsView.SelectedValue;
            if (selectedChannel != null && selectedChannel.ResponseData.Count > 0)
            {
                DrawChart(selectedChannel);
                //responseCombo.SelectedIndex = 0;
            }
        }

        private void rbtnXRangeOther_Checked(object sender, RoutedEventArgs e)
        {
            XMax = 20000;
            XMin = 15;
            selectedChannel = (DetectedChannel)channelsView.SelectedValue;
            if (selectedChannel != null && selectedChannel.ResponseData.Count > 0)
            {
                DrawChart(selectedChannel);
                //responseCombo.SelectedIndex = 0;
            }
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
        }
    }
}
