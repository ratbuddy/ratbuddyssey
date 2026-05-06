using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Audyssey.MultEQApp;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
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

        // Hover crosshair: a vertical line that follows the mouse and shows
        // the frequency under the pointer, REW-style. Re-added on every
        // DrawChart() because the underlying Plot.Clear() wipes plottables.
        private ScottPlot.Plottables.VerticalLine _crosshair;
        private bool _crosshairWired;

        private XRange selectedXRange = XRange.Full;
        private static readonly Dictionary<XRange, AxisLimit> AxisLimits = new()
        {
            // Y range is anchored around the normalization target (75 dB — see
            // NormalizeToReferenceDb). 45–95 gives ±20 dB of headroom which is
            // generous for in-room measurements; bumps and dips beyond that are
            // pathological and worth being clipped so they're noticed.
            { XRange.Full,      new AxisLimit { XMin = 20, XMax = 20000, YMin = 45, YMax = 95, YShift = 0, MajorStep = 5,    MinorStep = 1 } },
            { XRange.Subwoofer, new AxisLimit { XMin = 10, XMax = 1000,  YMin = 45, YMax = 95, YShift = 0, MajorStep = 5,    MinorStep = 1 } },
            { XRange.Chirp,     new AxisLimit { XMin = 0,  XMax = 350,   YMin = -0.1, YMax = 0.1, YShift = 0, MajorStep = 0.01, MinorStep = 0.001 } },
        };

        private readonly ObservableCollection<MeasurementSlot> measurementSlotItems = BuildMeasurementSlots();

        // Wong (2011) 8-color colorblind-safe palette. Reordered so the
        // first slot is the high-contrast anchor color and adjacent slots stay
        // distinguishable for deuteranopia/protanopia/tritanopia viewers.
        // Reference: https://www.nature.com/articles/nmeth.1618
        private static readonly (string Label, string ColorName)[] MeasurementSlotDescriptors =
        {
            ("1", "#000000"), // Black
            ("2", "#0072B2"), // Blue
            ("3", "#E69F00"), // Orange
            ("4", "#009E73"), // Green
            ("5", "#CC79A7"), // Reddish purple
            ("6", "#D55E00"), // Vermillion
            ("7", "#56B4E9"), // Sky blue
            ("8", "#F0E442"), // Yellow
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

        /// <summary>
        /// The first measurement slot uses pure black for max contrast against
        /// the white plot. In dark mode that's invisible, so swap it to a soft
        /// off-white. Called from <c>ApplyPlotTheme</c>.
        /// </summary>
        private void AdaptFirstMeasurementSlotToTheme(bool dark)
        {
            if (measurementSlotItems.Count == 0) return;
            var slot = measurementSlotItems[0];
            var newBrush = (IBrush)Brush.Parse(dark ? "#F5F5F5" : "#000000");
            slot.Brush = newBrush;
            if (slot.IsChecked) measurementColors[slot.Index] = BrushToColor(newBrush);
        }

        private void DrawChart()
        {
            if (plot == null)
            {
                Trace.TraceWarning("DrawChart skipped: plot control is null.");
                return;
            }
            plot.Plot.Clear();

            int linesAdded = 0;
            if (selectedChannel != null) { PlotLine(selectedChannel); linesAdded++; }
            foreach (var channel in stickyChannel)
            {
                if (channel.Sticky) { PlotLine(channel, secondaryChannel: true); linesAdded++; }
            }
            if (_viewModel.AudysseyMultEQApp != null)
            {
                switch (_viewModel.AudysseyMultEQApp.EnTargetCurveType)
                {
                    case 0: break;
                    case 1: PlotLine(null, false); linesAdded++; break;
                    case 2: PlotLine(null, true); linesAdded++; break;
                    default:
                        PlotLine(null, false);
                        PlotLine(null, true);
                        linesAdded += 2;
                        break;
                }
            }
            OverlayTargetCurve();
            OverlayAveragedResponse();
            OverlayRewMeasurement();
            OverlayParametricPreview();
            ApplyAxes();
            // Sub channels and the projected target both pile content into
            // the bass region, which is where the legend lives by default.
            // Move it up top while viewing a sub so it doesn't sit on top of
            // the LP rolloff and the data trace.
            plot.Plot.Legend.Alignment = selectedChannel != null
                && Audyssey.AudysseyHardwareQuirks.IsSubwoofer(selectedChannel)
                ? ScottPlot.Alignment.UpperRight
                : ScottPlot.Alignment.LowerRight;
            AddCrosshair();
            plot.Refresh();
            Trace.TraceInformation("DrawChart: lines={0}, selectedChannel={1}, sticky={2}, measurementKeys=[{3}], xRange={4}, logX={5}",
                linesAdded,
                selectedChannel?.CommandId ?? "(null)",
                stickyChannel.Count,
                string.Join(",", measurementKeys),
                selectedXRange,
                chbxLogarithmicAxis?.IsChecked == true);
        }

        // Vivid violet, deliberately outside the Wong measurement palette and
        // the red reference-curve color so the user-defined target stands out.
        private static readonly ScottPlot.Color TargetCurveColor = new(156, 39, 176);

        // A lighter violet for the "projected" curve — same hue family as the
        // user target so the eye groups them, but visually subordinate so the
        // editable target stays the dominant element on the chart.
        private static readonly ScottPlot.Color ProjectedTargetCurveColor = new(186, 104, 200);

        // Audyssey's midrange compensation dips the target a small amount
        // around 2 kHz to counter the perceived brightness of
        // flat-measured loudspeakers in real rooms. Public docs put the
        // depth at "about 2 dB" with the centre near 2 kHz; we model it as a
        // Gaussian on a log-frequency axis so it tapers smoothly into the
        // surrounding band rather than being a sharp notch.
        private const double MidrangeCompCenterHz = 2000.0;
        private const double MidrangeCompDepthDb = 2.0;
        // ~0.18 in log10(Hz) gives roughly a half-octave 1-sigma half-width,
        // which matches the visible dip on Audyssey app screenshots.
        private const double MidrangeCompLogSigma = 0.18;

        /// <summary>
        /// Overlays the selected channel's <c>CustomTargetCurvePointsDictionary</c>
        /// on the chart as a thick, marker-decorated line. Audyssey draws straight
        /// segments between adjacent points (in dB on a log-frequency axis), so we
        /// emit exactly the points the user typed and let the line renderer
        /// connect them — no interpolation needed here.
        /// </summary>
        private void OverlayTargetCurve()
        {
            if (chbxShowTargetCurve?.IsChecked != true) return;
            if (selectedXRange == XRange.Chirp) return; // target curves are frequency-domain only
            if (selectedChannel == null) return;

            bool logX = chbxLogarithmicAxis?.IsChecked == true;
            var pts = selectedChannel.CustomTargetCurvePointsDictionary;

            var ordered = new List<(double Hz, double Db)>();
            if (pts != null)
            {
                foreach (var p in pts)
                {
                    if (!double.TryParse(p.Key, NumberStyles.Float, CultureInfo.InvariantCulture, out double hz)) continue;
                    if (!double.TryParse(p.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double db)) continue;
                    ordered.Add((hz, db));
                }
                ordered.Sort((a, b) => a.Hz.CompareTo(b.Hz));
            }

            bool hasUserPoints = ordered.Count >= 1;
            if (ordered.Count == 1)
            {
                // A single user point is ambiguous on its own; extend it to the
                // chart edges as a horizontal target so the projected dip has
                // a curve to ride on and the line is actually visible.
                var only = ordered[0];
                var limits1 = AxisLimits[selectedXRange];
                ordered.Insert(0, (Math.Min(limits1.XMin, only.Hz), only.Db));
                ordered.Add((Math.Max(limits1.XMax, only.Hz), only.Db));
            }
            else if (ordered.Count == 0)
            {
                // No user points: stand in a flat 0 dB target so the user
                // always sees a target line (and so midrange-comp / rolloff
                // overlays have a baseline to modulate).
                var limits0 = AxisLimits[selectedXRange];
                ordered.Add((limits0.XMin, 0.0));
                ordered.Add((limits0.XMax, 0.0));
            }

            var xs = new double[ordered.Count];
            var ys = new double[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                xs[i] = logX ? Math.Log10(Math.Max(1e-9, ordered[i].Hz)) : ordered[i].Hz;
                // Target curve points are stored as relative-dB offsets (e.g. 0, -2);
                // add the same 75 dB anchor we apply to measurements so they
                // share a y-axis. See ComputeMidbandOffsetDb.
                ys[i] = ordered[i].Db + NormalizeToReferenceDb;
            }

            var line = plot.Plot.Add.Scatter(xs, ys);
            line.Color = TargetCurveColor;
            line.LineWidth = 2.5f;
            // Hide markers on the synthetic flat baseline — they'd imply
            // editable points the user didn't actually create.
            line.MarkerSize = hasUserPoints ? 7 : 0;
            line.MarkerShape = ScottPlot.MarkerShape.FilledCircle;
            line.LegendText = hasUserPoints ? "Target curve" : "Target curve (flat)";

            OverlayProjectedTargetCurve(ordered, logX);
        }

        /// <summary>
        /// Renders a secondary "projected" target curve showing the effective
        /// processing target after Audyssey's <c>MidrangeCompensation</c> dip
        /// and <c>FrequencyRangeRolloff</c> cutoff are applied. This is a
        /// preview only — nothing here is written back to the file. We render
        /// it as a thinner dashed line in a lighter shade of the target colour
        /// so the eye reads "same family, less prominent" against the editable
        /// user curve.
        /// </summary>
        /// <param name="userPoints">The user's target points, sorted ascending by Hz.</param>
        /// <param name="logX">Whether the chart's X axis is currently logarithmic.</param>
        private void OverlayProjectedTargetCurve(List<(double Hz, double Db)> userPoints, bool logX)
        {
            if (selectedChannel == null || userPoints.Count < 2) return;

            bool midrangeComp = selectedChannel.MidrangeCompensation == true;
            decimal? rolloff = selectedChannel.FrequencyRangeRolloff;
            // Audyssey's default for full-range channels is 20 kHz, which is
            // effectively "no cutoff" — don't bother with the projected line
            // unless midrange comp is on or the rolloff is set lower.
            bool hasRolloff = rolloff.HasValue && rolloff.Value > 0m && (double)rolloff.Value < 19500.0;

            // Bass management: speakers marked "Small" with a numeric crossover
            // get a high-pass at fc; subs get a low-pass at the highest fc among
            // the bass-managed speakers (so the visible target rolloff matches
            // what the AVR is actually summing into the LFE bus).
            bool isSub = Audyssey.AudysseyHardwareQuirks.IsSubwoofer(selectedChannel);
            double crossoverHz = ResolveCrossoverHz(selectedChannel, isSub);
            bool hasCrossover = crossoverHz > 0;

            if (!midrangeComp && !hasRolloff && !hasCrossover) return;

            var limits = AxisLimits[selectedXRange];
            double sampleLoHz = Math.Max(limits.XMin, userPoints[0].Hz);
            double sampleHiHz = Math.Min(limits.XMax, userPoints[^1].Hz);
            if (hasRolloff) sampleHiHz = Math.Min(sampleHiHz, (double)rolloff!.Value);
            if (sampleHiHz <= sampleLoHz) return;

            // Fine log-spaced sampling so the midrange dip renders smoothly
            // instead of as a piecewise-linear approximation of itself.
            const int sampleCount = 200;
            double logLo = Math.Log10(sampleLoHz);
            double logHi = Math.Log10(sampleHiHz);
            var xs = new double[sampleCount];
            var ys = new double[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                double t = (double)i / (sampleCount - 1);
                double hz = Math.Pow(10, logLo + t * (logHi - logLo));
                double baseDb = InterpolateUserCurveDb(userPoints, hz);
                if (midrangeComp)
                {
                    double z = (Math.Log10(hz) - Math.Log10(MidrangeCompCenterHz)) / MidrangeCompLogSigma;
                    baseDb -= MidrangeCompDepthDb * Math.Exp(-0.5 * z * z);
                }
                if (hasCrossover)
                {
                    baseDb += CrossoverFilterDb(hz, crossoverHz, isSub);
                }
                xs[i] = logX ? Math.Log10(hz) : hz;
                ys[i] = baseDb + NormalizeToReferenceDb;
            }

            var projected = plot.Plot.Add.Scatter(xs, ys);
            projected.Color = ProjectedTargetCurveColor;
            projected.LineWidth = 2.0f;
            projected.LinePattern = ScottPlot.LinePattern.Dashed;
            projected.MarkerSize = 0;
            projected.LegendText = BuildProjectedLegend(midrangeComp, hasRolloff, rolloff, hasCrossover, crossoverHz, isSub);

            // Vertical marker at the rolloff so the user can see where MultEQ
            // stops correcting. Drawn after the projected line so it sits on top.
            if (hasRolloff)
            {
                double rolloffHz = (double)rolloff!.Value;
                if (rolloffHz >= limits.XMin && rolloffHz <= limits.XMax)
                {
                    double xMark = logX ? Math.Log10(rolloffHz) : rolloffHz;
                    var vline = plot.Plot.Add.VerticalLine(xMark);
                    vline.Color = ProjectedTargetCurveColor;
                    vline.LineWidth = 1.5f;
                    vline.LinePattern = ScottPlot.LinePattern.Dotted;
                    vline.Text = $"correction stops at {rolloffHz:N0} Hz";
                    vline.LabelOppositeAxis = false;
                    // The label anchors at the line and grows outward; flip the
                    // alignment so it stays on-screen when the marker sits near
                    // an edge of the visible X-range.
                    double xLo = logX ? Math.Log10(limits.XMin) : limits.XMin;
                    double xHi = logX ? Math.Log10(limits.XMax) : limits.XMax;
                    double frac = (xMark - xLo) / (xHi - xLo);
                    vline.LabelAlignment = frac > 0.7
                        ? ScottPlot.Alignment.MiddleRight
                        : ScottPlot.Alignment.MiddleLeft;
                }
            }
        }

        /// <summary>
        /// Returns the crossover frequency (Hz) that should be reflected in
        /// the projected target curve, or 0 when bass-management isn't being
        /// applied. Non-sub channels use their own <c>CustomCrossover</c> when
        /// set to "Small". Sub channels use the highest crossover among any
        /// bass-managed speaker (they receive the summed low-pass content).
        /// </summary>
        private double ResolveCrossoverHz(DetectedChannel channel, bool isSub)
        {
            if (!isSub)
            {
                // Only Small ("S") channels are high-passed. Large ("L") plays
                // full-range, " " is unconfigured, "E" is a non-bass-managed
                // variant we don't want to roll off.
                if (!string.Equals(channel.CustomSpeakerType, "S", StringComparison.OrdinalIgnoreCase))
                    return 0;
                return TryParseHz(channel.CustomCrossover);
            }

            // Sub: walk every other channel and pick the highest crossover
            // that's actually being routed to the LFE bus.
            var app = _viewModel?.AudysseyMultEQApp;
            if (app?.DetectedChannels == null) return 0;
            double highest = 0;
            foreach (var ch in app.DetectedChannels)
            {
                if (ch == null || Audyssey.AudysseyHardwareQuirks.IsSubwoofer(ch)) continue;
                if (!string.Equals(ch.CustomSpeakerType, "S", StringComparison.OrdinalIgnoreCase)) continue;
                double xo = TryParseHz(ch.CustomCrossover);
                if (xo > highest) highest = xo;
            }
            return highest;
        }

        private static double TryParseHz(string s) =>
            decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal d) && d > 0
                ? (double)d
                : 0;

        /// <summary>
        /// 4th-order Linkwitz-Riley filter response at frequency <paramref name="hz"/>
        /// for crossover <paramref name="fc"/>. LR4 = two cascaded 2nd-order
        /// Butterworth sections, so |H|² for the high-pass at f is
        /// (f/fc)^8 / (1 + (f/fc)^8); the dB form simplifies to
        /// −10·log10(1 + (fc/f)^8). The low-pass is the same with f and fc
        /// swapped. Both pass-through 0 dB at the crossover (sum = 0 dB).
        /// </summary>
        private static double CrossoverFilterDb(double hz, double fc, bool lowPass)
        {
            if (hz <= 0 || fc <= 0) return 0;
            double ratio = lowPass ? hz / fc : fc / hz;
            double r8 = Math.Pow(ratio, 8);
            return -10.0 * Math.Log10(1.0 + r8);
        }

        private static string BuildProjectedLegend(bool midrangeComp, bool hasRolloff, decimal? rolloff,
                                                   bool hasCrossover, double crossoverHz, bool isSub)
        {
            var parts = new List<string>(3);
            if (hasCrossover)
                parts.Add($"{(isSub ? "LP" : "HP")} @ {crossoverHz:N0} Hz");
            if (midrangeComp) parts.Add("mid-comp");
            if (hasRolloff) parts.Add($"rolloff @ {rolloff:N0} Hz");
            return parts.Count == 0
                ? "Projected"
                : "Projected (" + string.Join(" + ", parts) + ")";
        }

        /// <summary>
        /// Linear-in-frequency dB interpolation between adjacent target points.
        /// Clamps to the endpoints outside the curve's range. Matches the
        /// straight-segment rendering Audyssey itself uses between target points.
        /// </summary>
        private static double InterpolateUserCurveDb(List<(double Hz, double Db)> sorted, double hz)
        {
            if (hz <= sorted[0].Hz) return sorted[0].Db;
            if (hz >= sorted[^1].Hz) return sorted[^1].Db;
            for (int i = 1; i < sorted.Count; i++)
            {
                var a = sorted[i - 1];
                var b = sorted[i];
                if (hz <= b.Hz)
                {
                    double t = (hz - a.Hz) / (b.Hz - a.Hz);
                    return a.Db + t * (b.Db - a.Db);
                }
            }
            return sorted[^1].Db;
        }

        private void ShowTargetCurve_Changed(object sender, RoutedEventArgs e) => DrawChart();

        // -- Modernization overlays ------------------------------------------

        // Olive-green: distinct from Wong palette and the violet target curve.
        private static readonly ScottPlot.Color AveragedResponseColor = new(33, 150, 83);

        // Burnt orange: pre-imported REW exports usually live alongside the
        // Audyssey traces, so we want a hue that's clearly "not Audyssey".
        private static readonly ScottPlot.Color RewOverlayColor = new(255, 112, 67);

        // Slightly darker violet than the editable target so a parametric
        // preview reads as "candidate target" without being confused for the
        // user-curve overlay above it.
        private static readonly ScottPlot.Color ParametricPreviewColor = new(123, 31, 162);

        /// <summary>
        /// When the user has the "Show averaged response" option enabled, render
        /// an extra spectrum trace that is the *incoherent* (magnitude-domain)
        /// average of every checked mic position for the selected channel.
        ///
        /// We deliberately average the per-position magnitude spectra rather
        /// than calling <c>ResponseAveraging.GetAveragedChannelResponse</c>
        /// (time-domain coherent average): coherent averaging across mic
        /// positions causes phase cancellation that depresses the midband,
        /// which our 75 dB midband normalization then over-corrects, putting
        /// the average visibly above the per-position traces. Magnitude
        /// averaging is the standard "spatial average" curve and lines up
        /// with the individual traces the way users expect.
        /// </summary>
        private void OverlayAveragedResponse()
        {
            if (_viewModel?.ShowAveragedResponse != true) return;
            if (selectedChannel == null) return;
            if (selectedXRange == XRange.Chirp) return; // only meaningful in frequency domain

            // Use whatever the user currently has checked; fall back to every
            // available position when nothing is selected so the overlay still
            // renders something sensible.
            var keys = measurementKeys.Count > 0
                ? new List<int>(measurementKeys)
                : new List<int>();
            if (keys.Count == 0 && selectedChannel.ResponseData != null)
            {
                foreach (var k in selectedChannel.ResponseData.Keys)
                {
                    if (int.TryParse(k, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
                        keys.Add(idx);
                }
            }
            if (keys.Count == 0) return;

            int half = 0;
            double[] sumMag = null;
            double[] freqs = null;
            int contributions = 0;

            foreach (int k in keys)
            {
                string key = k.ToString(CultureInfo.InvariantCulture);
                if (selectedChannel.ResponseData == null
                    || !selectedChannel.ResponseData.TryGetValue(key, out var values)
                    || values == null || values.Length == 0)
                {
                    continue;
                }

                var (cValues, fxs) = ChartDataPrep.BuildSpectrumInput(values);
                MathNet.Numerics.IntegralTransforms.Fourier.Forward(cValues);

                if (sumMag == null)
                {
                    half = cValues.Length / 2;
                    if (half == 0) return;
                    sumMag = new double[half];
                    freqs = fxs;
                }
                int n = Math.Min(half, cValues.Length / 2);
                for (int i = 0; i < n; i++) sumMag[i] += cValues[i].Magnitude;
                contributions++;
            }
            if (sumMag == null || contributions == 0) return;

            double inv = 1.0 / contributions;
            for (int i = 0; i < half; i++) sumMag[i] *= inv;

            if (smoothingFactor != 0)
            {
                LinSpacedFracOctaveSmooth(smoothingFactor, ref sumMag, 1, 1d / 48);
            }

            double offset = ComputeMidbandOffsetDb(sumMag, freqs, selectedChannel);
            bool logX = chbxLogarithmicAxis?.IsChecked == true;
            var limits = AxisLimits[selectedXRange];

            var xs = new double[half];
            var ys = new double[half];
            for (int i = 0; i < half; i++)
            {
                double f = freqs[i];
                xs[i] = logX ? Math.Log10(Math.Max(1e-9, f)) : f;
                ys[i] = limits.YShift + 20 * Math.Log10(Math.Max(1e-30, sumMag[i])) + offset;
            }

            var line = plot.Plot.Add.Scatter(xs, ys);
            line.Color = AveragedResponseColor;
            line.LineWidth = 2.5f;
            line.MarkerSize = 0;
            line.LegendText = $"Averaged response ({contributions} pos.)";
        }

        /// <summary>
        /// Renders the imported REW measurement as an SPL overlay. We treat REW
        /// values as already calibrated dB SPL: no normalization to the 75 dB
        /// reference, since the whole point of the import is to compare against
        /// a known measurement.
        /// </summary>
        private void OverlayRewMeasurement()
        {
            if (_viewModel?.ShowRewOverlay != true) return;
            if (selectedXRange == XRange.Chirp) return;
            var meas = _viewModel.RewMeasurement;
            if (meas?.Points == null || meas.Points.Count == 0) return;

            bool logX = chbxLogarithmicAxis?.IsChecked == true;
            var (rxs, rys) = ChartDataPrep.BuildRewOverlaySeries(meas);
            if (logX)
            {
                for (int i = 0; i < rxs.Length; i++) rxs[i] = Math.Log10(Math.Max(1e-9, rxs[i]));
            }

            var line = plot.Plot.Add.Scatter(rxs, rys);
            line.Color = RewOverlayColor;
            line.LineWidth = 2.0f;
            line.MarkerSize = 0;
            line.LinePattern = ScottPlot.LinePattern.Dashed;
            line.LegendText = string.IsNullOrEmpty(meas.Name) ? "REW overlay" : $"REW: {meas.Name}";
        }

        /// <summary>
        /// Overlays a parametric-curve preview as a dashed series. Anchored to the
        /// same 75 dB midband reference as the user target so the preview lines up
        /// with the measurement traces.
        /// </summary>
        private void OverlayParametricPreview()
        {
            var preview = _viewModel?.PreviewCurvePoints;
            if (preview == null || preview.Count == 0) return;
            if (selectedXRange == XRange.Chirp) return;

            bool logX = chbxLogarithmicAxis?.IsChecked == true;
            var xs = new double[preview.Count];
            var ys = new double[preview.Count];
            for (int i = 0; i < preview.Count; i++)
            {
                double hz = preview[i].FrequencyHz;
                xs[i] = logX ? Math.Log10(Math.Max(1e-9, hz)) : hz;
                ys[i] = preview[i].GainDb + NormalizeToReferenceDb;
            }

            var line = plot.Plot.Add.Scatter(xs, ys);
            line.Color = ParametricPreviewColor;
            line.LineWidth = 2.0f;
            line.LinePattern = ScottPlot.LinePattern.Dotted;
            line.MarkerSize = 5;
            line.MarkerShape = ScottPlot.MarkerShape.OpenCircle;
            line.LegendText = "Parametric preview";
        }

        // -- end overlays ----------------------------------------------------

        // Re-renders the chart when the user toggles MidrangeCompensation,
        // changes the crossover/speaker-type combos, or edits FrequencyRangeRolloff
        // so the dashed "projected" line updates live. Deferred via Dispatcher
        // because Avalonia raises IsCheckedChanged / SelectionChanged before the
        // two-way binding has finished writing the new value back into the model;
        // calling DrawChart synchronously would render with the *previous* state
        // and require a second toggle to catch up.
        private void OnTargetProjectionInputChanged(object sender, RoutedEventArgs e) =>
            Dispatcher.UIThread.Post(DrawChart, DispatcherPriority.Background);

        /// <summary>
        /// Adds a hidden vertical-line plottable that the pointer-move handler
        /// will reposition + reveal as the user mouses over the graph. Called
        /// at the tail of DrawChart because Plot.Clear wipes everything; the
        /// pointer handlers are wired exactly once on first draw.
        /// </summary>
        private void AddCrosshair()
        {
            if (plot == null) return;
            _crosshair = plot.Plot.Add.VerticalLine(0);
            _crosshair.IsVisible = false;
            _crosshair.Color = new ScottPlot.Color(120, 120, 120, 200);
            _crosshair.LineWidth = 1f;
            _crosshair.LinePattern = ScottPlot.LinePattern.Dotted;
            _crosshair.LabelOppositeAxis = false;
            _crosshair.LabelAlignment = ScottPlot.Alignment.LowerLeft;
            _crosshair.ExcludeFromLegend = true;

            if (_crosshairWired) return;
            _crosshairWired = true;
            plot.PointerMoved += OnPlotPointerMoved;
            plot.PointerExited += OnPlotPointerExited;
        }

        private void OnPlotPointerMoved(object sender, Avalonia.Input.PointerEventArgs e)
        {
            if (plot == null || _crosshair == null) return;
            var pos = e.GetPosition(plot);
            // Avalonia delivers DIPs; ScottPlot 5 already accounts for DPI in
            // GetCoordinates so we can pass the raw point through.
            var px = new ScottPlot.Pixel((float)pos.X, (float)pos.Y);
            var coord = plot.Plot.GetCoordinates(px);

            bool isChirp = selectedXRange == XRange.Chirp;
            bool logX = chbxLogarithmicAxis?.IsChecked == true && !isChirp;

            // Convert the X axis position back to Hz for the label. When the
            // axis is in log10 space the plottable's Position is log10(Hz);
            // otherwise it's the raw axis unit (Hz, or seconds for chirp).
            double xAxis = coord.X;
            double labelHz = logX ? Math.Pow(10, xAxis) : xAxis;

            var limits = AxisLimits[selectedXRange];
            double xLo = logX ? Math.Log10(limits.XMin) : limits.XMin;
            double xHi = logX ? Math.Log10(limits.XMax) : limits.XMax;
            if (xAxis < xLo || xAxis > xHi || coord.Y < limits.YMin || coord.Y > limits.YMax)
            {
                if (_crosshair.IsVisible)
                {
                    _crosshair.IsVisible = false;
                    plot.Refresh();
                }
                return;
            }

            _crosshair.Position = xAxis;
            _crosshair.IsVisible = true;
            // Use a dedicated kHz/Hz format for the hover label rather than
            // the compact axis-tick FormatHz ("1k") so the readout is clear.
            string hzLabel = labelHz >= 1000
                ? $"{labelHz / 1000.0:0.##} kHz"
                : $"{labelHz:0.#} Hz";
            _crosshair.Text = isChirp
                ? $"{labelHz * 1000.0:N1} ms, {coord.Y:N3}"
                : $"{hzLabel}, {coord.Y:N1} dB";
            // Keep the label inside the plot when the cursor is near the right
            // edge — same trick as the rolloff marker.
            double frac = (xAxis - xLo) / (xHi - xLo);
            _crosshair.LabelAlignment = frac > 0.7
                ? ScottPlot.Alignment.LowerRight
                : ScottPlot.Alignment.LowerLeft;
            plot.Refresh();
        }

        private void OnPlotPointerExited(object sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_crosshair == null) return;
            if (_crosshair.IsVisible)
            {
                _crosshair.IsVisible = false;
                plot.Refresh();
            }
        }

        private void ApplyAxes()
        {
            if (plot == null)
            {
                Trace.TraceWarning("ApplyAxes skipped: plot is null.");
                return;
            }
            var limits = AxisLimits[selectedXRange];
            bool isChirp = selectedXRange == XRange.Chirp;
            bool logX = chbxLogarithmicAxis?.IsChecked == true && !isChirp;

            plot.Plot.Axes.Bottom.Label.Text = isChirp ? "ms" : "Hz";
            plot.Plot.Axes.Left.Label.Text = isChirp ? string.Empty : "dB";

            double xLo, xHi, yLo, yHi;
            if (logX)
            {
                // Hand-rolled ticks so the user gets readable density across
                // each decade: labeled majors at the decade boundaries (plus
                // the chart endpoints), unlabeled minors at every 10/100/1000
                // step within the visible range.
                plot.Plot.Axes.Bottom.TickGenerator = BuildLogFrequencyTicks(limits.XMin, limits.XMax);
                xLo = Math.Log10(Math.Max(1, limits.XMin));
                xHi = Math.Log10(limits.XMax);
                yLo = limits.YMin + limits.YShift;
                yHi = limits.YMax + limits.YShift;
            }
            else
            {
                plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
                xLo = limits.XMin;
                xHi = limits.XMax;
                yLo = limits.YMin + (isChirp ? 0 : limits.YShift);
                yHi = limits.YMax + (isChirp ? 0 : limits.YShift);
            }
            plot.Plot.Axes.SetLimits(xLo, xHi, yLo, yHi);
            Trace.TraceInformation("ApplyAxes: range={0}, logX={1}, limits=[{2:G4}..{3:G4}, {4:G4}..{5:G4}]",
                selectedXRange, logX, xLo, xHi, yLo, yHi);
        }

        /// <summary>
        /// Builds a log-frequency tick generator with denser, more readable
        /// gridlines than ScottPlot's automatic decade-only default. Major
        /// (labeled) ticks land on each decade plus the chart endpoints;
        /// minor (unlabeled) ticks land at every 10 Hz step in 10–100, every
        /// 100 Hz in 100–1000, every 1 kHz in 1k–10k, and every 1 kHz again
        /// in 10k–20k. All positions are stored in log10 space because
        /// ApplyAxes runs the axis with logX coordinates.
        /// </summary>
        private static ScottPlot.TickGenerators.NumericManual BuildLogFrequencyTicks(double xMinHz, double xMaxHz)
        {
            var majors = new List<ScottPlot.Tick>();
            var minors = new List<ScottPlot.Tick>();

            // Decade-boundary majors (10, 100, 1000, 10000) that fall inside
            // the visible range.
            for (double dec = 10; dec <= 100000; dec *= 10)
            {
                if (dec >= xMinHz && dec <= xMaxHz)
                    majors.Add(new ScottPlot.Tick(Math.Log10(dec), FormatHz(dec), isMajor: true));
            }

            // Endpoint majors (e.g. 20 / 20000) so the user can see exactly
            // where the chart starts and ends. Skip if they coincide with a
            // decade we already labeled.
            void AddEndpoint(double hz)
            {
                if (hz <= 0) return;
                double pos = Math.Log10(hz);
                if (majors.Any(t => Math.Abs(t.Position - pos) < 1e-6)) return;
                majors.Add(new ScottPlot.Tick(pos, FormatHz(hz), isMajor: true));
            }
            AddEndpoint(xMinHz);
            AddEndpoint(xMaxHz);

            // Minor ticks within each decade.
            void AddMinors(double from, double to, double step)
            {
                for (double v = from; v <= to + step * 0.5; v += step)
                {
                    if (v < xMinHz || v > xMaxHz) continue;
                    if (majors.Any(t => Math.Abs(t.Position - Math.Log10(v)) < 1e-6)) continue;
                    minors.Add(new ScottPlot.Tick(Math.Log10(v), string.Empty, isMajor: false));
                }
            }
            AddMinors(10, 100, 10);
            AddMinors(100, 1000, 100);
            AddMinors(1000, 10000, 1000);
            AddMinors(10000, 20000, 1000);

            var all = majors.Concat(minors).OrderBy(t => t.Position).ToArray();
            return new ScottPlot.TickGenerators.NumericManual(all);
        }

        private static string FormatHz(double hz) =>
            hz >= 1000
                ? (hz / 1000).ToString("0.###", CultureInfo.InvariantCulture) + "k"
                : hz.ToString("0", CultureInfo.InvariantCulture);

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
                    // Reference roll-off points are relative-dB; share the
                    // 75 dB anchor with measurements and target curve.
                    ys[i] = refPoints[i].Y + NormalizeToReferenceDb;
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
                    var mag = new double[half];
                    if (smoothingFactor == 0)
                    {
                        for (int x = 0; x < half; x++) mag[x] = cValues[x].Magnitude;
                    }
                    else
                    {
                        var smoothed = cValues.Select(c => c.Magnitude).ToArray();
                        LinSpacedFracOctaveSmooth(smoothingFactor, ref smoothed, 1, 1d / 48);
                        for (int x = 0; x < half; x++) mag[x] = smoothed[x];
                    }

                    // Per-trace midband normalization to a 75 dB reference (see
                    // NormalizeToReferenceDb for rationale). This is intentionally
                    // not user-toggleable: raw FFT magnitude isn't dBFS, dB SPL, or
                    // anything else interpretable, and every comparable tool
                    // (REW, Dirac, the Audyssey app, Trinnov) normalizes the same
                    // way. A toggle would just be UI weight without a use case.
                    double offset = ComputeMidbandOffsetDb(mag, xs, channel);

                    xList = new List<double>(half);
                    yList = new List<double>(half);
                    for (int x = 0; x < half; x++)
                    {
                        double freq = xs[x];
                        xList.Add(logX ? Math.Log10(Math.Max(1e-9, freq)) : freq);
                        yList.Add(limits.YShift + 20 * Math.Log10(Math.Max(1e-30, mag[x])) + offset);
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

        /// <summary>
        /// Reference SPL we anchor each normalized trace to. 75 dB makes the
        /// numbers feel like SPL even though they aren't truly calibrated
        /// (we have no mic correction file), matching the convention used by
        /// the Audyssey app and most room-EQ tools.
        /// </summary>
        private const double NormalizeToReferenceDb = 75.0;

        /// <summary>
        /// Returns the dB offset that, when added to <c>20*log10(mag[x])</c>,
        /// places this trace's midband average at <see cref="NormalizeToReferenceDb"/>.
        /// Subwoofer channels are anchored to 30–80 Hz (their actual pass-band);
        /// everything else uses 500 Hz – 2 kHz, the standard "presence" band
        /// used by REW / Dirac / Audyssey.
        /// Falls back to a wider band, then to no offset, if the preferred band
        /// is empty (e.g. extremely short response data).
        /// </summary>
        private static double ComputeMidbandOffsetDb(double[] mag, double[] freqs, DetectedChannel channel)
        {
            bool isSub = Audyssey.AudysseyHardwareQuirks.IsSubwoofer(channel);
            double bandLo = isSub ? 30.0 : 500.0;
            double bandHi = isSub ? 80.0 : 2000.0;

            double avg = AverageDbOverBand(mag, freqs, bandLo, bandHi);
            if (double.IsNaN(avg))
            {
                // Fallback: widen the band before giving up entirely.
                avg = AverageDbOverBand(mag, freqs, isSub ? 20.0 : 200.0, isSub ? 200.0 : 5000.0);
            }
            if (double.IsNaN(avg)) return 0.0;
            return NormalizeToReferenceDb - avg;
        }

        private static double AverageDbOverBand(double[] mag, double[] freqs, double loHz, double hiHz)
        {
            int half = Math.Min(mag.Length, freqs.Length / 1); // freqs is full length, mag is half
            int n = 0;
            double sumDb = 0.0;
            for (int i = 0; i < mag.Length && i < freqs.Length; i++)
            {
                double f = freqs[i];
                if (f < loHz || f > hiHz) continue;
                double m = Math.Max(1e-30, mag[i]);
                sumDb += 20.0 * Math.Log10(m);
                n++;
            }
            return n == 0 ? double.NaN : sumDb / n;
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

        private DetectedChannel SelectedDetectedChannel()
            => UnwrapChannel(channelsView?.SelectedItem);

        private static DetectedChannel UnwrapChannel(object selectedItem)
            => selectedItem switch
            {
                ChannelRowViewModel row => row?.Channel,
                DetectedChannel ch => ch,
                _ => null,
            };

        private void ChannelsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                foreach (var slot in measurementSlotItems) slot.IsEnabled = false;

                // Prefer e.AddedItems[0] — it's the row that was just clicked,
                // and is set even when SelectionChanged fires before the
                // DataGrid's own SelectedItem getter is ready (which throws
                // NRE in some Avalonia 11 / DataGrid 11.3 internal states).
                object selObj = e?.AddedItems != null && e.AddedItems.Count > 0
                    ? e.AddedItems[0]
                    : (sender as DataGrid)?.SelectedItem;
                var sel = UnwrapChannel(selObj);

                Trace.TraceInformation("ChannelsView_SelectionChanged: selectedItemType={0}, commandId={1}, hasResponseData={2}, keys={3}",
                    selObj?.GetType().Name ?? "null",
                    sel?.CommandId ?? "(null)",
                    sel?.ResponseData != null,
                    sel?.ResponseData?.Count ?? -1);

                if (sel != null && sel.ResponseData != null)
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
                        AutoSelectXRangeForChannel(sel);
                        AutoSelectSmoothingForChannel(sel);
                        DrawChart();
                    }
                    else
                    {
                        Trace.TraceWarning("Channel '{0}' has no measurement keys; nothing to plot.", sel.CommandId);
                    }
                }

                foreach (var slot in measurementSlotItems)
                {
                    if (!slot.IsEnabled && slot.IsChecked) slot.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("ChannelsView_SelectionChanged failed: {0}", ex);
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
            var ch = SelectedDetectedChannel();
            if (ch != null &&
                !string.IsNullOrEmpty(keyTbx.Text) && !string.IsNullOrEmpty(valueTbx.Text))
            {
                ch.CustomTargetCurvePointsDictionary.Add(new MyKeyValuePair(keyTbx.Text, valueTbx.Text));
                DrawChart();
            }
        }

        private void ButtonClickRemoveTargetCurvePoint(object sender, RoutedEventArgs e)
        {
            var ch = SelectedDetectedChannel();
            if (sender is Button b && b.DataContext is MyKeyValuePair pair && ch != null)
            {
                ch.CustomTargetCurvePointsDictionary.Remove(pair);
                DrawChart();
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
                if (!_suppressXRangeRedraw) DrawChart();
            }
        }

        // Set true around programmatic radio-button updates so the resulting
        // IsCheckedChanged storm doesn't trigger a redraw before our caller
        // (e.g. ChannelsView_SelectionChanged) does its own.
        private bool _suppressXRangeRedraw;

        /// <summary>
        /// When the user clicks a different channel, switch the chart between
        /// the full-range (20 Hz – 20 kHz) and subwoofer (10 – 1000 Hz)
        /// frequency views automatically. Leaves the impulse-response view
        /// alone — if the user has chosen to inspect timing, that's a
        /// deliberate choice we shouldn't second-guess.
        /// </summary>
        private void AutoSelectXRangeForChannel(DetectedChannel channel)
        {
            if (channel == null) return;
            if (selectedXRange == XRange.Chirp) return;

            var desired = Audyssey.AudysseyHardwareQuirks.IsSubwoofer(channel)
                ? XRange.Subwoofer
                : XRange.Full;
            if (desired == selectedXRange) return;

            var target = desired switch
            {
                XRange.Subwoofer => rbXRangeSubwoofer,
                XRange.Full => rbXRangeFull,
                _ => null
            };
            if (target == null || target.IsChecked == true) return;

            _suppressXRangeRedraw = true;
            try { target.IsChecked = true; } // updates selectedXRange via XRangeChanged
            finally { _suppressXRangeRedraw = false; }
        }

        /// <summary>
        /// Subs benefit from heavy fractional-octave smoothing (1/48) so room
        /// modes don't drown out the broad shape; mains read better at 1/12.
        /// Mirrors <see cref="AutoSelectXRangeForChannel"/>: switch on channel
        /// change unless the user is in the impulse-response view.
        /// </summary>
        private void AutoSelectSmoothingForChannel(DetectedChannel channel)
        {
            if (channel == null) return;
            if (selectedXRange == XRange.Chirp) return;

            var target = Audyssey.AudysseyHardwareQuirks.IsSubwoofer(channel)
                ? rbSmoothing48
                : rbSmoothing12;
            if (target == null || target.IsChecked == true) return;
            target.IsChecked = true; // RadioButtonSmoothingFactorChanged updates smoothingFactor
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

        [ObservableProperty]
        private IBrush _brush;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private bool _isEnabled = true;

        public MeasurementSlot(int index, string label, IBrush brush)
        {
            Index = index;
            Label = label;
            _brush = brush;
            // All measurement positions are visible by default; the user can
            // uncheck individual slots to focus on a single position.
            _isChecked = true;
        }
    }
}
