#nullable disable
using System.Collections.Generic;
using Audyssey;
using Audyssey.MultEQApp;

namespace Ratbuddyssey.Tests;

public class AudysseyHardwareQuirksTests
{
    [Theory]
    [InlineData("Marantz SR8012", 300.0)]
    [InlineData("Denon AVR-X3700H", 300.0)]
    [InlineData("Denon AVR-X3800H", 343.0)] // not in list
    [InlineData("Marantz NR1711", 300.0)]
    [InlineData("Marantz Cinema 50", 343.0)]
    [InlineData("", 343.0)]
    [InlineData(null, 343.0)]
    [InlineData("AV8805", 300.0)] // exactly 6 chars
    public void GetSpeedOfSoundMps_MatchesAudysseyOneList(string model, double expected)
    {
        Assert.Equal(expected, AudysseyHardwareQuirks.GetSpeedOfSoundMps(model));
    }

    [Theory]
    [InlineData(35, 80)]   // below floor → raised to 80
    [InlineData(60, 80)]   // below floor → raised to 80
    [InlineData(80, 80)]
    [InlineData(85, 80)]
    [InlineData(95, 90)]
    [InlineData(115, 110)]
    [InlineData(140, 150)]
    [InlineData(220, 200)]
    [InlineData(260, 250)]
    [InlineData(9999, 250)]
    public void SnapCrossoverHz_SnapsToNearestAndEnforcesFloor(double raw, int expected)
    {
        Assert.Equal(expected, AudysseyHardwareQuirks.SnapCrossoverHz(raw));
    }

    [Theory]
    [InlineData(35.0, 40, 40)]    // explicit lower floor allows 40 Hz
    [InlineData(55.0, 40, 60)]    // closer to 60
    [InlineData(70.0, 60, 60)]
    public void SnapCrossoverHz_RespectsCustomFloor(double raw, int floor, int expected)
    {
        Assert.Equal(expected, AudysseyHardwareQuirks.SnapCrossoverHz(raw, floor));
    }

    [Fact]
    public void SubwooferHelpers_ScanDetectedChannels()
    {
        var channels = new List<DetectedChannel>
        {
            new() { CommandId = "FL", DelayAdjustment = "0.0", TrimAdjustment = "-1.5" },
            new() { CommandId = "SW1", DelayAdjustment = "2.5", TrimAdjustment = "-3.0" },
            new() { CommandId = "SW2", DelayAdjustment = "1.0", TrimAdjustment = "-7.5" },
        };

        // Headroom = 6 - max(2.5, 1.0) = 3.5
        Assert.Equal(3.5m, AudysseyHardwareQuirks.GetSubwooferDelayHeadroomMeters(channels));
        // Floor = -(12 + min(-3, -7.5)) / 2 = -(12 + -7.5)/2 = -2.25
        Assert.Equal(-2.25m, AudysseyHardwareQuirks.GetSubwooferTrimFloorDb(channels));
    }

    [Fact]
    public void IsSubwoofer_DetectsSwPrefix()
    {
        Assert.True(AudysseyHardwareQuirks.IsSubwoofer(new DetectedChannel { CommandId = "SW1" }));
        Assert.True(AudysseyHardwareQuirks.IsSubwoofer(new DetectedChannel { CommandId = "SW2" }));
        Assert.False(AudysseyHardwareQuirks.IsSubwoofer(new DetectedChannel { CommandId = "FL" }));
        Assert.False(AudysseyHardwareQuirks.IsSubwoofer(new DetectedChannel { CommandId = null }));
        Assert.False(AudysseyHardwareQuirks.IsSubwoofer(null));
    }
}
