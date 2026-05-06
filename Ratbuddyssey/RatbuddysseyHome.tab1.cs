using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Audyssey.MultEQApp;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;

namespace Ratbuddyssey
{
    public enum XRange
    {
        Full,
        Subwoofer,
        Chirp,
    }

    public partial class RatbuddysseyHome
    {
        private readonly List<int> measurementKeys = new();
        private readonly Dictionary<int, ScottPlot.Color> measurementColors = new();

        private double smoothingFactor;

        private DetectedChannel selectedChannel;
        private readonly List<DetectedChannel> stickyChannel = new();

        private XRange selectedXRange = XRange.Full;
        private static readonly Dictionary<XRange, AxisLimit> AxisLimits = new()
        {
            { XRange.Full,      new AxisLimit { XMin = 10, XMax = 24000, YMin = -35, YMax = 20, YShift = 0, MajorStep = 5,    MinorStep = 1 } },
            { XRange.Subwoofer, new AxisLimit { XMin = 10, XMax = 1000,  YMin = -35, YMax = 20, YShift = 0, MajorStep = 5,    MinorStep = 1 } },
            { XRange.Chirp,     new AxisLimit { XMin = 0,  XMax = 350,   YMin = -0.1, YMax = 0.1, YShift = 0, MajorStep = 0.01, MinorStep = 0.001 } },
        };

        private readonly ObservableCollection<MeasurementSlot> measurementSlotItems = BuildMeasurementSlots();

        private static readonly (string Label, string ColorName)[] MeasurementSlotDescriptors =
        {
            ("1", "Black"),
            ("2", "Blue"),
            ("3", "Violet"),
            ("4", "Green"),
            ("5", "Orange"),
            ("6", "Red"),
            ("7", "Cyan"),
            ("8", "DeepPink"),
        };

        private static ObservableCollection<MeasurementSlot> BuildMeasurementSlots()
        {
            var slots = new ObservableCollection<MeasurementSlot>();
            for (int i = 0; i < MeasurementSlotDescriptors.Length; i++)
            {
                var (label, colorName) = MeasurementSlotDescriptors[i];
                var brush = (IBrush)(Brush.Parse(colorName));
                slots.Add(new MeasurementSlot(i, label, brush));
            }
            return slots;
        }

        // 61-band Audyssey filter centre frequencies. Drives both the slider strip in
        // BuildFilterSliders() and any future band-aware feature; do not reorder.
        private static readonly string[] FilterBandLabels =
        {
            "20Hz","22Hz","25Hz","28Hz","31Hz","35Hz","39Hz","44Hz","50Hz","56Hz",
            "62.5Hz","70Hz","79Hz","88Hz","100Hz","111Hz","125Hz","140Hz","157.5Hz","177Hz",
            "200Hz","223Hz","250","280","315","354","400","445","500","561",
            "630","707","800","890","1kHz","1.1kHz","1.2kHz","1.4kHz","1.6kHz","1.8kHz",
            "2kHz","2.2kHz","2.5kHz","2.8kHz","3.2kHz","3.6kHz","4kHz","4.5kHz","5kHz","5.6kHz",
            "6.4kHz","7.2kHz","8kHz","9kHz","10kHz","11.2kHz","12.8kHz","14.4kHz","16kHz","18kHz","20kHz"
        };

        private void BuildFilterSliders()
        {
            WireMeasurementSlots();

            var panel = this.FindControl<StackPanel>("FilBandsPanel");
            if (panel == null) return;
            for (int i = 0; i < FilterBandLabels.Length; i++)
            {
                var sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical };
                var slider = new Slider
                {
                    Height = 150,
                    Minimum = -20,
                    Maximum = 10,
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    TickPlacement = TickPlacement.Outside,
                    LargeChange = 5,
                    SmallChange = 1
                };
                slider.Bind(Slider.ValueProperty, new Avalonia.Data.Binding($"SelectedDisFil.FilData[{i}]"));
                sp.Children.Add(slider);
                sp.Children.Add(new TextBlock { Text = FilterBandLabels[i], FontSize = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center });
                panel.Children.Add(sp);
            }
        }

        private void WireMeasurementSlots()
        {
            var itemsControl = this.FindControl<ItemsControl>("measurementSlots");
            if (itemsControl != null) itemsControl.ItemsSource = measurementSlotItems;

            // Idempotent: clear cached state and re-subscribe so calling this twice
            // doesn't double-fire OnMeasurementSlotPropertyChanged or duplicate keys.
            measurementKeys.Clear();
            measurementColors.Clear();
            foreach (var slot in measurementSlotItems)
            {
                slot.PropertyChanged -= OnMeasurementSlotPropertyChanged;
                if (slot.IsChecked)
                {
                    measurementKeys.Add(slot.Index);
                    measurementColors[slot.Index] = BrushToColor(slot.Brush);
                }
                slot.PropertyChanged += OnMeasurementSlotPropertyChanged;
            }
        }

        private void OnMeasurementSlotPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MeasurementSlot.IsChecked) && sender is MeasurementSlot slot)
            {
                CheckBoxMeasurementPositionChanged(slot);
            }
        }

        private static ScottPlot.Color BrushToColor(IBrush brush)
        {
            if (brush is ISolidColorBrush scb)
            {
                var c = scb.Color;
                return new ScottPlot.Color(c.R, c.G, c.B, c.A);
            }
            return ScottPlot.Colors.Black;
        }

        private void DrawChart()
        {
            if (plot == null) return;
            plot.Plot.Clear();

            if (selectedChannel != null) PlotLine(selectedChannel);
            foreach (var channel in stickyChannel)
            {
                if (channel.Sticky) PlotLine(channel, secondaryChannel: true);
            }
            if (_viewModel.AudysseyMultEQApp != null)
            {
                switch (_viewModel.AudysseyMultEQApp.EnTargetCurveType)
                {
                    case 0: break;
                    case 1: PlotLine(null, false); break;
                    case 2: PlotLine(null, true); break;
                    default:
                        PlotLine(null, false);
                        PlotLine(null, true);
                        break;
                }
            }
            ApplyAxes();
            plot.Refresh();
        }

        private void ApplyAxes()
        {
            var limits = AxisLimits[selectedXRange];
            bool isChirp = selectedXRange == XRange.Chirp;
            bool logX = chbxLogarithmicAxis?.IsChecked == true && !isChirp;

            plot.Plot.Axes.Bottom.Label.Text = isChirp ? "ms" : "Hz";
            plot.Plot.Axes.Left.Label.Text = isChirp ? string.Empty : "dB";

            if (logX)
            {
                var minorTickGen = new ScottPlot.TickGenerators.LogMinorTickGenerator();
                var tickGen = new ScottPlot.TickGenerators.NumericAutomatic
                {
                    MinorTickGenerator = minorTickGen,
                    IntegerTicksOnly = true,
                    LabelFormatter = v => Math.Pow(10, v).ToString("N0", CultureInfo.InvariantCulture)
                };
                plot.Plot.Axes.Bottom.TickGenerator = tickGen;
                plot.Plot.Axes.SetLimits(
                    Math.Log10(Math.Max(1, limits.XMin)),
                    Math.Log10(limits.XMax),
                    limits.YMin + limits.YShift,
                    limits.YMax + limits.YShift);
            }
            else
            {
                plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
                plot.Plot.Axes.SetLimits(
                    limits.XMin,
                    limits.XMax,
                    limits.YMin + (isChirp ? 0 : limits.YShift),
                    limits.YMax + (isChirp ? 0 : limits.YShift));
            }
        }

        private void PlotLine(DetectedChannel channel, bool secondaryChannel = false)
        {
            bool logX = chbxLogarithmicAxis?.IsChecked == true && selectedXRange != XRange.Chirp;

            if (channel == null)
            {
                var refPoints = secondaryChannel
                    ? audysseyMultEQReferenceCurveFilter.HighFrequencyRollOff2()
                    : audysseyMultEQReferenceCurveFilter.HighFrequencyRollOff1();
                if (refPoints == null || refPoints.Count == 0) return;

                double[] xs = new double[refPoints.Count];
                double[] ys = new double[refPoints.Count];
                for (int i = 0; i < refPoints.Count; i++)
                {
                    xs[i] = logX ? Math.Log10(Math.Max(1e-9, refPoints[i].X)) : refPoints[i].X;
                    ys[i] = refPoints[i].Y;
                }
                var line = plot.Plot.Add.Scatter(xs, ys);
                line.Color = ScottPlot.Colors.Red;
                line.LineWidth = 2;
                line.MarkerSize = 0;
                return;
            }

            for (int i = 0; i < measurementKeys.Count; i++)
            {
                string key = measurementKeys[i].ToString(CultureInfo.InvariantCulture);
                if (channel.ResponseData == null || !channel.ResponseData.ContainsKey(key)) continue;

                string[] values = channel.ResponseData[key];
                if (values == null || values.Length == 0) continue;

                int count = values.Length;
                var limits = AxisLimits[selectedXRange];

                List<double> xList;
                List<double> yList;

                if (selectedXRange == XRange.Chirp)
                {
                    var (xs, ys) = ChartDataPrep.BuildChirpSeries(values);
                    xList = new List<double>(xs);
                    yList = new List<double>(ys);
                }
                else
                {
                    var (cValues, xs) = ChartDataPrep.BuildSpectrumInput(values);
                    MathNet.Numerics.IntegralTransforms.Fourier.Forward(cValues);

                    int half = count / 2;
                    xList = new List<double>(half);
                    yList = new List<double>(half);
                    if (smoothingFactor == 0)
                    {
                        for (int x = 0; x < half; x++)
                        {
                            double freq = xs[x];
                            xList.Add(logX ? Math.Log10(Math.Max(1e-9, freq)) : freq);
                            yList.Add(limits.YShift + 20 * Math.Log10(cValues[x].Magnitude));
                        }
                    }
                    else
                    {
                        var smoothed = cValues.Select(c => c.Magnitude).ToArray();
                        LinSpacedFracOctaveSmooth(smoothingFactor, ref smoothed, 1, 1d / 48);
                        for (int x = 0; x < half; x++)
                        {
                            double freq = xs[x];
                            xList.Add(logX ? Math.Log10(Math.Max(1e-9, freq)) : freq);
                            yList.Add(limits.YShift + 20 * Math.Log10(smoothed[x]));
                        }
                    }
                }

                var color = measurementColors.TryGetValue(measurementKeys[i], out var c) ? c : ScottPlot.Colors.Black;
                var series = plot.Plot.Add.Scatter(xList.ToArray(), yList.ToArray());
                series.Color = color;
                series.LineWidth = 1;
                series.MarkerSize = 0;
                series.LinePattern = secondaryChannel ? LinePattern.Dotted : LinePattern.Solid;
            }
        }

        private static void LinSpacedFracOctaveSmooth(double frac, ref double[] smoothed, float startFreq, double freqStep)
        {
            const int passes = 8;
            double scaledFrac = 7.5 * frac;
            double octMult = Math.Pow(2, 0.5 / scaledFrac);
            double bwFactor = octMult - 1 / octMult;
            double b = 0.5 + bwFactor * startFreq / freqStep;
            int N = smoothed.Length;
            for (int pass = 0; pass < passes; pass++)
            {
                double xp = smoothed[N - 1];
                double yp = xp;
                for (int i = N - 2; i >= 0; i--)
                {
                    double a = 1 / (b + i * bwFactor);
                    yp += ((xp + smoothed[i]) / 2 - yp) * a;
                    xp = smoothed[i];
                    smoothed[i] = (float)yp;
                }
                for (int i = 1; i < N; i++)
                {
                    double a = 1 / (b + i * bwFactor);
                    yp += ((xp + smoothed[i]) / 2 - yp) * a;
                    xp = smoothed[i];
                    smoothed[i] = (float)yp;
                }
            }
        }

        private void CheckBoxMeasurementPositionChanged(MeasurementSlot slot)
        {
            int val = slot.Index;
            if (!slot.IsChecked)
            {
                if (measurementKeys.Remove(val))
                {
                    measurementColors.Remove(val);
                    DrawChart();
                }
                return;
            }

            if (selectedChannel != null && (selectedChannel.ResponseData == null ||
                !selectedChannel.ResponseData.ContainsKey(val.ToString(CultureInfo.InvariantCulture))))
            {
                slot.IsChecked = false;
                return;
            }
            if (!measurementKeys.Contains(val))
            {
                measurementKeys.Add(val);
                measurementColors[val] = BrushToColor(slot.Brush);
                DrawChart();
            }
        }

        private void AllCheckBoxMeasurementPositionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            bool target = cb.IsChecked == true;
            foreach (var slot in measurementSlotItems)
            {
                if (slot.IsEnabled || !target) slot.IsChecked = target;
            }
            DrawChart();
        }

        private void ChannelsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var slot in measurementSlotItems) slot.IsEnabled = false;

            if (channelsView.SelectedItem is DetectedChannel sel && sel.ResponseData != null)
            {
                foreach (var measurementPosition in sel.ResponseData)
                {
                    if (int.TryParse(measurementPosition.Key, out int idx) && idx >= 0 && idx < measurementSlotItems.Count)
                    {
                        measurementSlotItems[idx].IsEnabled = true;
                    }
                }
                if (sel.ResponseData.Count > 0)
                {
                    selectedChannel = sel;
                    RefreshStickyChannels();
                    DrawChart();
                }
            }

            foreach (var slot in measurementSlotItems)
            {
                if (!slot.IsEnabled && slot.IsChecked) slot.IsChecked = false;
            }
        }

        private void RefreshStickyChannels()
        {
            stickyChannel.Clear();
            if (_viewModel.AudysseyMultEQApp?.DetectedChannels == null) return;
            foreach (var channel in _viewModel.AudysseyMultEQApp.DetectedChannels)
            {
                if (channel.Sticky) stickyChannel.Add(channel);
            }
        }

        private void ButtonClickAddTargetCurvePoint(object sender, RoutedEventArgs e)
        {
            if (channelsView.SelectedItem is DetectedChannel ch &&
                !string.IsNullOrEmpty(keyTbx.Text) && !string.IsNullOrEmpty(valueTbx.Text))
            {
                ch.CustomTargetCurvePointsDictionary.Add(new MyKeyValuePair(keyTbx.Text, valueTbx.Text));
            }
        }

        private void ButtonClickRemoveTargetCurvePoint(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is MyKeyValuePair pair &&
                channelsView.SelectedItem is DetectedChannel ch)
            {
                ch.CustomTargetCurvePointsDictionary.Remove(pair);
            }
        }

        private void RadioButtonSmoothingFactorChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb || rb.IsChecked != true) return;
            if (rb.Tag is string tag && double.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out double f))
            {
                smoothingFactor = f;
                DrawChart();
            }
        }

        private void XRangeChanged(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true && rb.Tag is string tag &&
                Enum.TryParse<XRange>(tag, out var range))
            {
                selectedXRange = range;
                DrawChart();
            }
        }

        private void chbxLogarithmicAxis_Changed(object sender, RoutedEventArgs e) => DrawChart();

        private void TargetCurveTypeSelectionChanged(object sender, SelectionChangedEventArgs e) => DrawChart();
    }

    internal sealed class AxisLimit
    {
        public double XMin { get; set; }
        public double XMax { get; set; }
        public double YMin { get; set; }
        public double YMax { get; set; }
        public double YShift { get; set; }
        public double MajorStep { get; set; }
        public double MinorStep { get; set; }
    }

    public partial class MeasurementSlot : ObservableObject
    {
        public int Index { get; }
        public string Label { get; }
        public IBrush Brush { get; }

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private bool _isEnabled = true;

        public MeasurementSlot(int index, string label, IBrush brush)
        {
            Index = index;
            Label = label;
            Brush = brush;
            // First slot starts checked to match prior UI default.
            _isChecked = index == 0;
        }
    }
}
