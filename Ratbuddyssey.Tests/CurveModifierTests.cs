#nullable disable
#pragma warning disable CA1305, CA1859
using System.Collections.Generic;
using Ratbuddyssey;
using Ratbuddyssey.Audio.Curves;
using Xunit;

namespace Ratbuddyssey.Tests
{
    public class CurveModifierTests
    {
        private static readonly IReadOnlyList<HouseCurves.Point> Base = new[]
        {
            new HouseCurves.Point(20,    6),
            new HouseCurves.Point(120,   0),
            new HouseCurves.Point(1000,  0),
            new HouseCurves.Point(10000,-6),
        };

        [Fact]
        public void Strength_Zero_With_No_Modifiers_Returns_Flat()
        {
            var s = new CurveSettings { Strength = 0.0 };
            var result = CurveModifier.ApplyCurveSettings(Base, s);
            Assert.All(result, p => Assert.Equal(0.0, p.GainDb, 6));
        }

        [Fact]
        public void Strength_One_With_No_Modifiers_Returns_Base()
        {
            var s = new CurveSettings { Strength = 1.0 };
            var result = CurveModifier.ApplyCurveSettings(Base, s);
            Assert.Equal(Base.Count, result.Count);
            for (int i = 0; i < Base.Count; i++)
            {
                Assert.Equal(Base[i].FrequencyHz, result[i].FrequencyHz);
                Assert.Equal(Base[i].GainDb, result[i].GainDb, 6);
            }
        }

        [Fact]
        public void Strength_Half_Scales_Base_Gain()
        {
            var s = new CurveSettings { Strength = 0.5 };
            var result = CurveModifier.ApplyCurveSettings(Base, s);
            Assert.Equal(3.0, result[0].GainDb, 6);   // 6 * 0.5
            Assert.Equal(-3.0, result[3].GainDb, 6);  // -6 * 0.5
        }

        [Fact]
        public void Strength_Clamps_To_Range()
        {
            var s = new CurveSettings { Strength = 5.0 };
            Assert.Equal(1.0, s.Strength);
            s.Strength = -1.0;
            Assert.Equal(0.0, s.Strength);
        }

        [Fact]
        public void BassBoost_Full_At_Or_Below_20Hz_Zero_At_Or_Above_200Hz()
        {
            var pts = new[]
            {
                new HouseCurves.Point(20, 0),
                new HouseCurves.Point(200, 0),
                new HouseCurves.Point(1000, 0),
            };
            var s = new CurveSettings { Strength = 0.0, BassBoostDb = 6.0 };
            var result = CurveModifier.ApplyCurveSettings(pts, s);
            Assert.Equal(6.0, result[0].GainDb, 6);
            Assert.Equal(0.0, result[1].GainDb, 6);
            Assert.Equal(0.0, result[2].GainDb, 6);
        }

        [Fact]
        public void TrebleTilt_Zero_Below_2k_Full_At_20k()
        {
            var pts = new[]
            {
                new HouseCurves.Point(1000, 0),
                new HouseCurves.Point(2000, 0),
                new HouseCurves.Point(20000, 0),
            };
            var s = new CurveSettings { Strength = 0.0, TrebleTiltDb = -4.0 };
            var result = CurveModifier.ApplyCurveSettings(pts, s);
            Assert.Equal(0.0, result[0].GainDb, 6);
            Assert.Equal(0.0, result[1].GainDb, 6);
            Assert.Equal(-4.0, result[2].GainDb, 6);
        }

        [Fact]
        public void Returns_New_List_And_Does_Not_Mutate_Input()
        {
            var original = new List<HouseCurves.Point>(Base);
            var s = new CurveSettings { Strength = 0.5, BassBoostDb = 3, TrebleTiltDb = -2 };
            var result = CurveModifier.ApplyCurveSettings(original, s);
            Assert.NotSame(original, result);
            // Input untouched
            for (int i = 0; i < Base.Count; i++)
            {
                Assert.Equal(Base[i].FrequencyHz, original[i].FrequencyHz);
                Assert.Equal(Base[i].GainDb, original[i].GainDb);
            }
        }

        [Fact]
        public void Null_Base_Returns_Empty()
        {
            var result = CurveModifier.ApplyCurveSettings(null, new CurveSettings());
            Assert.Empty(result);
        }

        [Fact]
        public void Null_Settings_Treated_As_Defaults()
        {
            var result = CurveModifier.ApplyCurveSettings(Base, null);
            // Default Strength=1, no boost/tilt → equals base.
            for (int i = 0; i < Base.Count; i++)
                Assert.Equal(Base[i].GainDb, result[i].GainDb, 6);
        }

        [Fact]
        public void HouseCurves_GetPreset_Returns_Unmodified_Reference()
        {
            var preset = HouseCurves.GetPreset(HouseCurves.All[1]); // Harman
            Assert.Same(HouseCurves.All[1].Points, preset);
        }

        [Fact]
        public void HouseCurves_GetPresetWithSettings_Returns_New_Curve()
        {
            var harman = HouseCurves.All[1];
            var s = new CurveSettings { Strength = 0.5 };
            var modified = HouseCurves.GetPresetWithSettings(harman, s);
            Assert.NotSame(harman.Points, modified);
            Assert.Equal(harman.Points.Count, modified.Count);
            for (int i = 0; i < harman.Points.Count; i++)
                Assert.Equal(harman.Points[i].GainDb * 0.5, modified[i].GainDb, 6);
            // Preset itself untouched.
            Assert.Equal(6.0, harman.Points[0].GainDb);
        }
    }
}
