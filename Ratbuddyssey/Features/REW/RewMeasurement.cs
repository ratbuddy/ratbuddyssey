using System.Collections.Generic;

namespace Ratbuddyssey.Features.REW
{
    /// <summary>
    /// Single (frequency Hz, SPL dB) point from a REW measurement export.
    /// Phase is intentionally omitted — the overlay needs magnitude only.
    /// </summary>
    public readonly record struct MeasurementPoint(double FrequencyHz, double SplDb);

    /// <summary>
    /// Read-only frequency-response measurement imported from REW. Used purely
    /// as overlay data on the existing chart; never written back to the .ady file.
    /// </summary>
    public sealed class RewMeasurement
    {
        public string Name { get; set; }
        public IReadOnlyList<MeasurementPoint> Points { get; set; } = System.Array.Empty<MeasurementPoint>();
    }

    /// <summary>
    /// Outcome of a parser run. Either <see cref="Measurement"/> is non-null,
    /// or <see cref="Error"/> is non-null. Never both, never neither.
    /// </summary>
    public sealed class RewParseResult
    {
        public RewMeasurement Measurement { get; init; }
        public string Error { get; init; }
        public int LinesRead { get; init; }
        public int LinesSkipped { get; init; }

        public bool Success => Measurement != null;
    }
}
