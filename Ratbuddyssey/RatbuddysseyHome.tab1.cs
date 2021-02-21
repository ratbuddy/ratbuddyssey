using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Audyssey.MultEQApp;

namespace Ratbuddyssey
{
    public partial class RatbuddysseyHome : Page
    {
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
                if (audysseyMultEQApp != null)
                {
                    switch (audysseyMultEQApp.EnTargetCurveType)
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
                        points = audysseyMultEQReferenceCurveFilter.High_Frequency_Roll_Off_2();
                    }
                    else
                    {
                        points = audysseyMultEQReferenceCurveFilter.High_Frequency_Roll_Off_1();
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
                    if (!selectedChannel.ResponseData.ContainsKey(s))
                        continue;

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
                        if (radioButtonSmoothingFactorNone.IsChecked.Value)
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
        private void CheckBoxMeasurementPositionUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            int val = int.Parse(checkBox.Content.ToString()) - 1;
            if (keys.Contains(val))
            {
                keys.Remove(val);
                colors.Remove(val);
                DrawChart();
            }
        }
        private void CheckBoxMeasurementPositionChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            int val = int.Parse(checkBox.Content.ToString()) - 1;
            if (selectedChannel != null && !selectedChannel.ResponseData.ContainsKey(val.ToString()))
            {
                // This channel has not been measured in this Audyssey calibration. Don't attempt to plot it, and clear the checkbox.
                checkBox.IsChecked = false;
            }
            else if (!keys.Contains(val))
            {
                keys.Add(val);
                colors.Add(val, checkBox.Foreground);
                DrawChart();
            }
        }
        private void AllCheckBoxMeasurementPositionChecked(object sender, RoutedEventArgs e)
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
        private void AllCheckBoxMeasurementPositionUnchecked(object sender, RoutedEventArgs e)
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
        private void ChannelsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<CheckBox> checkBoxes = new List<CheckBox> {
                chbx1, chbx2, chbx3, chbx4, chbx5, chbx6, chbx7, chbx8
            };

            // Disable all the check boxes
            foreach (var checkBox in checkBoxes)
            {
                checkBox.IsEnabled = false;
            }

            var selectedValue = channelsView.SelectedValue as DetectedChannel;
            if (selectedValue != null && selectedValue.ResponseData != null)
            {
                // Enable the check boxes corresponding to those positions for which the measurement is available
                foreach (var measurementPosition in selectedValue.ResponseData)
                {
                    int positionIndex = int.Parse(measurementPosition.Key);
                    Debug.Assert(positionIndex >= 0 && positionIndex < checkBoxes.Count);
                    checkBoxes[positionIndex].IsEnabled = true;
                }

                if (selectedValue.ResponseData.Count > 0)
                {
                    selectedChannel = (DetectedChannel)channelsView.SelectedValue;
                    DrawChart();
                }
            }

            // Un-check all the disabled check boxes
            foreach (var checkBox in checkBoxes)
            {
                if (!checkBox.IsEnabled && checkBox.IsChecked == true)
                    checkBox.IsChecked = false;
            }
        }
        private void ChannelsView_OnClickSticky(object sender, RoutedEventArgs e)
        {
            foreach (var channel in audysseyMultEQApp.DetectedChannels)
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
        private void ButtonClickAddTargetCurvePoint(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(keyTbx.Text) && !string.IsNullOrEmpty(valueTbx.Text))
            {
                ((DetectedChannel)channelsView.SelectedValue).CustomTargetCurvePointsDictionary.Add(new MyKeyValuePair(keyTbx.Text, valueTbx.Text));
            }
        }
        private void ButtonClickRemoveTargetCurvePoint(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            MyKeyValuePair pair = b.DataContext as MyKeyValuePair;
            ((DetectedChannel)channelsView.SelectedValue).CustomTargetCurvePointsDictionary.Remove(pair);            
        }
        private void RadioButtonSmoothingFactorChecked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            switch (radioButton.Name)
            {
                case "radioButtonSmoothingFactorNone":
                    smoothingFactor = 1;
                    break;
                case "radioButtonSmoothingFactor2":
                    smoothingFactor = 2;
                    break;
                case "radioButtonSmoothingFactor3":
                    smoothingFactor = 3;
                    break;
                case "radioButtonSmoothingFactor6":
                    smoothingFactor = 6;
                    break;
                case "radioButtonSmoothingFactor12":
                    smoothingFactor = 12;
                    break;
                case "radioButtonSmoothingFactor24":
                    smoothingFactor = 24;
                    break;
                case "radioButtonSmoothingFactor48":
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