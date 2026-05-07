using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace Audyssey
{
    /// <summary>
    /// Simple (x, y) point used for shipped reference-curve JSON files.
    /// Field names are lowercase to match the JSON on disk.
    /// </summary>
    public struct DataPoint
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        public DataPoint(double x, double y) { X = x; Y = y; }
    }

    /// <summary>
    /// Reads precomputed Audyssey reference roll-off curves from JSON files shipped alongside the executable.
    /// </summary>
    public class AudysseyMultEQReferenceCurveFilter
    {
        private readonly Collection<DataPoint> high_frequency_roll_off_1_points;
        private readonly Collection<DataPoint> high_frequency_roll_off_2_points;

        public AudysseyMultEQReferenceCurveFilter()
        {
            high_frequency_roll_off_1_points = LoadCurve("high_frequency_roll_off_1.json");
            high_frequency_roll_off_2_points = LoadCurve("high_frequency_roll_off_2.json");
        }

        public Collection<DataPoint> HighFrequencyRollOff1() => high_frequency_roll_off_1_points;
        public Collection<DataPoint> HighFrequencyRollOff2() => high_frequency_roll_off_2_points;

        private static Collection<DataPoint> LoadCurve(string fileName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, fileName);
            var points = ReadPointsFromJsonFile(path);
            if (points == null)
            {
                Trace.TraceWarning("Reference curve missing: {0}", path);
            }
            return points;
        }

        public static Collection<DataPoint> ReadPointsFromJsonFile(string filename)
        {
            if (!File.Exists(filename)) return null;

            // Defensive: refuse anything wildly out of expected size for these small curve files.
            var info = new FileInfo(filename);
            if (info.Length > 4L * 1024 * 1024) return null;

            string serialized = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<Collection<DataPoint>>(serialized);
        }
    }
}
