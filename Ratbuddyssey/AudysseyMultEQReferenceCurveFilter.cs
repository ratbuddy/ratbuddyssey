using System;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using OxyPlot;
using MathNet.Numerics.Interpolation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Audyssey
{
    public class AudysseyMultEQReferenceCurveFilter
    {
        readonly string high_frequency_roll_off_1_points_filename = "high_frequency_roll_off_1";
        readonly string high_frequency_roll_off_2_points_filename = "high_frequency_roll_off_2";

        Collection<DataPoint> high_frequency_roll_off_1_points = null;
        Collection<DataPoint> high_frequency_roll_off_2_points = null;

        public AudysseyMultEQReferenceCurveFilter()
        {
            high_frequency_roll_off_1_points_filename = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), high_frequency_roll_off_1_points_filename), "json");
            high_frequency_roll_off_1_points = ReadPointsFromJsonFile(high_frequency_roll_off_1_points_filename);
            if (high_frequency_roll_off_1_points == null)
            {
                string warning = high_frequency_roll_off_1_points_filename + " missing";
                MessageBox.Show(warning, "Json file reader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                high_frequency_roll_off_1_points_filename = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), high_frequency_roll_off_1_points_filename), "png");
                high_frequency_roll_off_1_points = GeneratePointsFromBitmap(high_frequency_roll_off_1_points_filename);
            }
            if (high_frequency_roll_off_1_points != null)
            {
                WritePointsToJsonFile(high_frequency_roll_off_1_points_filename, high_frequency_roll_off_1_points);
            }
            else
            {
                string warning = high_frequency_roll_off_1_points_filename + " missing";
                MessageBox.Show(warning, "Bitmap file reader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            high_frequency_roll_off_2_points_filename = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), high_frequency_roll_off_2_points_filename), "json");
            high_frequency_roll_off_2_points = ReadPointsFromJsonFile(high_frequency_roll_off_2_points_filename);
            if (high_frequency_roll_off_2_points == null)
            {
                string warning = high_frequency_roll_off_2_points_filename + " missing";
                MessageBox.Show(warning, "Json file reader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                high_frequency_roll_off_2_points_filename = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), high_frequency_roll_off_2_points_filename), "png");
                high_frequency_roll_off_2_points = GeneratePointsFromBitmap(high_frequency_roll_off_2_points_filename);
            }
            if (high_frequency_roll_off_2_points != null)
            {
                WritePointsToJsonFile(high_frequency_roll_off_2_points_filename, high_frequency_roll_off_2_points);
            }
            else
            {
                string warning = high_frequency_roll_off_2_points_filename + " missing";
                MessageBox.Show(warning, "Bitmap file reader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public Collection<DataPoint> High_Frequency_Roll_Off_1()
        {
            return high_frequency_roll_off_1_points;
        }
        public Collection<DataPoint> High_Frequency_Roll_Off_2()
        {
            return high_frequency_roll_off_2_points;
        }
        public Collection<DataPoint> ReadPointsFromJsonFile(string filename)
        {
            if (File.Exists(filename))
            {
                string Serialized = File.ReadAllText(filename);
                Collection<DataPoint> frequency_points = JsonConvert.DeserializeObject<Collection<OxyPlot.DataPoint>>(Serialized,
                    new JsonSerializerSettings
                    {
                        FloatParseHandling = FloatParseHandling.Decimal
                    });
                return frequency_points;
            }
            else
            {
                return null;
            }
        }
        public void WritePointsToJsonFile(string filename, Collection<DataPoint> frequency_points)
        {
            if (frequency_points != null)
            {
                string Serialized = JsonConvert.SerializeObject(frequency_points, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                File.WriteAllText(Path.ChangeExtension(filename, "json"), Serialized);
            }
        }
        public Collection<DataPoint> GeneratePointsFromBitmap(string filename)
        {
            filename = Path.ChangeExtension(filename, "png");
            if (File.Exists(filename))
            {
                double fmax = 19000.0;
                double fmin = 20.0;
                double dbrange = 16.0;
                double dbmax = 1;

                double sampleRate = 48000;
                int sampleSize = 16384;

                // Create a Bitmap object from an image file.
                Bitmap iBitmap = new Bitmap(filename);
                Bitmap oBitmap = new Bitmap(iBitmap.Width, iBitmap.Height);

                int first = 0;
                int last = 0;
                int size = 0;
                double min = iBitmap.Height;
                double max = 0;
                double mag = 0;

                // Get the color of a pixel within myBitmap.
                Collection<KeyValuePair<int, double>> points = new Collection<KeyValuePair<int, double>>();
                for (var x = 0; x < iBitmap.Width; x++)
                {
                    List<int> list = new List<int>();
                    for (var y = 0; y < iBitmap.Height; y++)
                    {
                        Color pixelColor = iBitmap.GetPixel(x, y);
                        if ((pixelColor.R > 200) && (pixelColor.G < 25) && (pixelColor.B < 25))
                        {
                            if (first == 0) first = x;
                            else last = x;
                            oBitmap.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                            list.Add(y);
                        }
                    }
                    if (list.Count > 0)
                    {
                        var a = iBitmap.Height - list.Average();
                        points.Add(new KeyValuePair<int, double>(x, a));
                        if (a < min) min = a;
                        if (a > max) max = a;
                    }
                }

                size = last - first + 1;
                mag = max - min + 1;

                //calculate gain & offset
                double scale = dbrange / mag;
                double shift = dbmax - max * scale;

                //check
                double n_min = min * scale + shift;
                double n_max = max * scale + shift;

                //convert bitmap to frequency spectrum            
                Collection<double> ex = new Collection<double>();
                Collection<double> ey = new Collection<double>();
                for (var x = 0; x < points.Count; x++)
                {
                    double df = Math.Log10(fmax / fmin); //over aproximately 3 decades
                    double f = 20.0 * Math.Pow(10.0, df * x / points.Count); //convert log horizontal image pixels to linear frequency scale in Hz
                    double y = points[x].Value * scale + shift;//scale and shift vertical image pixels to correct magnitude in dB

                    ex.Add(f);
                    ey.Add(Math.Pow(10.0, y / 20.0));
                }

                //interpolate frequency spectrum to match Audyssey responseData length
                double[] frequency = Enumerable.Range(0, sampleSize).Select(p => p * sampleRate / sampleSize).ToArray();
                CubicSpline IA = CubicSpline.InterpolateAkima(ex, ey);
                Collection<DataPoint> frequency_points = new Collection<DataPoint>();
                foreach (var f in frequency)
                {
                    if (f < fmin)
                    {   // exptrapolate
                        frequency_points.Add(new DataPoint(f, double.NegativeInfinity));
                    }
                    else
                    {
                        if (f > fmax)
                        {   // exptrapolate
                            frequency_points.Add(new DataPoint(f, double.NegativeInfinity));
                        }
                        else
                        {   //interpolate
                            frequency_points.Add(new DataPoint(f, 20 * Math.Log10(IA.Interpolate(f))));
                        }
                    }
                }

                oBitmap.Save(Path.ChangeExtension(filename, "jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);

                return frequency_points;
            }
            else
            {
                return null;
            }
        }
    }
}