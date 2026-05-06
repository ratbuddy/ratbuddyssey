using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Ratbuddyssey.Features.REW
{
    /// <summary>
    /// Tolerant parser for REW <c>.txt</c> frequency-response exports.
    ///
    /// REW writes a small header (a few lines starting with <c>*</c> or text)
    /// followed by data rows. Data rows are typically:
    /// <code>frequency_hz   spl_db   phase_deg</code>
    /// separated by tabs or runs of spaces. We accept a comma decimal in any
    /// numeric field so European locale exports also load. Phase is ignored.
    /// </summary>
    public sealed class RewTxtParser
    {
        /// <summary>Files with fewer valid points than this are rejected.</summary>
        public const int MinimumValidPoints = 8;

        private static readonly char[] FieldSeparators = { '\t', ' ', ';' };

        /// <summary>
        /// Parse <paramref name="path"/> and return either a populated
        /// <see cref="RewMeasurement"/> or an explanatory error. Never throws
        /// on malformed input — file-system errors (missing file, IO) surface
        /// as <see cref="RewParseResult.Error"/> rather than exceptions.
        /// </summary>
        public RewParseResult ParseFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return new RewParseResult { Error = "No path supplied." };

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
            {
                return new RewParseResult { Error = $"Could not read file: {ex.Message}" };
            }

            return Parse(lines, name: Path.GetFileNameWithoutExtension(path));
        }

        /// <summary>
        /// In-memory entry point. Exposed for tests; real callers use <see cref="ParseFile"/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Public API: kept as instance for symmetry with ParseFile and to allow future state.")]
        public RewParseResult Parse(IEnumerable<string> lines, string name = null)
        {
            if (lines == null) return new RewParseResult { Error = "No data." };

            var points = new List<MeasurementPoint>(1024);
            int read = 0;
            int skipped = 0;

            foreach (var raw in lines)
            {
                read++;
                if (string.IsNullOrWhiteSpace(raw)) { skipped++; continue; }

                string line = raw.Trim();
                // Common comment / header markers in REW and similar exports.
                if (line[0] == '*' || line[0] == '#' || line[0] == ';' || line[0] == '/')
                {
                    skipped++;
                    continue;
                }

                if (TryParseRow(line, out double hz, out double db))
                {
                    points.Add(new MeasurementPoint(hz, db));
                }
                else
                {
                    skipped++;
                }
            }

            if (points.Count < MinimumValidPoints)
            {
                return new RewParseResult
                {
                    Error = $"Only {points.Count} valid data point(s) found (need at least {MinimumValidPoints}).",
                    LinesRead = read,
                    LinesSkipped = skipped,
                };
            }

            return new RewParseResult
            {
                Measurement = new RewMeasurement { Name = name, Points = points },
                LinesRead = read,
                LinesSkipped = skipped,
            };
        }

        private static bool TryParseRow(string line, out double hz, out double db)
        {
            hz = 0; db = 0;

            // Comma decimal → dot decimal, but only when the line has no other
            // dot already in a number (avoids breaking 1,234.5 thousand-grouped
            // numbers — REW doesn't use grouping, so this is safe in practice).
            string normalized = line.Contains('.') ? line : line.Replace(',', '.');

            var parts = normalized.Split(FieldSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out hz))
                return false;
            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out db))
                return false;

            // Sanity bounds: REW exports cover roughly 1 Hz – 96 kHz; reject
            // obvious non-data rows that happened to start with two numbers.
            if (double.IsNaN(hz) || double.IsInfinity(hz) || hz <= 0 || hz > 200_000) return false;
            if (double.IsNaN(db) || double.IsInfinity(db) || db < -200 || db > 200) return false;

            return true;
        }
    }
}
