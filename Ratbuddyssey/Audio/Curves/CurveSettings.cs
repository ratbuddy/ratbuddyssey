namespace Ratbuddyssey.Audio.Curves
{
    /// <summary>
    /// Non-destructive parametric controls applied on top of a base house curve.
    /// Construct, set fields, hand to <see cref="CurveModifier.ApplyCurveSettings"/>;
    /// the original curve is never mutated.
    /// </summary>
    public sealed class CurveSettings
    {
        /// <summary>Bass shelf gain in dB applied gradually below ~200 Hz.</summary>
        public double BassBoostDb { get; set; }

        /// <summary>Treble tilt in dB applied gradually above ~2 kHz.</summary>
        public double TrebleTiltDb { get; set; }

        private double _strength = 1.0;

        /// <summary>
        /// Blend factor toward flat for the base curve's gain. Clamped to 0.0–1.0.
        /// 0 = flat (parametric modifiers still apply), 1 = full base curve plus modifiers.
        /// </summary>
        public double Strength
        {
            get => _strength;
            set
            {
                if (value < 0.0) _strength = 0.0;
                else if (value > 1.0) _strength = 1.0;
                else _strength = value;
            }
        }
    }
}
