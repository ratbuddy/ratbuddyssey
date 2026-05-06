#nullable disable
#pragma warning disable CA1861 // test fixtures use inline arrays for clarity
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Audyssey;
using Audyssey.MultEQApp;
using Xunit;

namespace Ratbuddyssey.Tests;

public class AudysseyOnePostProcessTests
{
    private static AudysseyMultEQApp BuildApp()
    {
        return new AudysseyMultEQApp
        {
            DynamicEq = true,
            DynamicVolume = true,
            Lfc = true,
            EnTargetCurveType = 0,
            DetectedChannels = new ObservableCollection<DetectedChannel>
            {
                new DetectedChannel
                {
                    CommandId = "FL",
                    MidrangeCompensation = true,
                    FrequencyRangeRolloff = 12000m,
                    CustomSpeakerType = "L",
                    ChannelReport = new ChannelReport { EnSpeakerConnect = 0 },
                    ResponseData = new Dictionary<string, string[]>
                    {
                        ["0"] = new[] { "0.5", "0.25", "0.1" },
                        ["1"] = new[] { "0.4", "0.2", "0.05" },
                    },
                },
                new DetectedChannel
                {
                    CommandId = "SW1",
                    FrequencyRangeRolloff = 80m,
                    TrimAdjustment = "-3",
                    DelayAdjustment = "1.2",
                    ResponseData = new Dictionary<string, string[]>
                    {
                        ["0"] = new[] { "0.9", "0.3", "0.1" },
                    },
                },
            },
        };
    }

    [Fact]
    public void Apply_ForcesPostProcessFlagsOff()
    {
        var app = BuildApp();
        AudysseyOnePostProcess.Apply(app);
        Assert.False(app.DynamicEq);
        Assert.False(app.DynamicVolume);
        Assert.False(app.Lfc);
        Assert.Equal(1, app.EnTargetCurveType);
    }

    [Fact]
    public void Apply_ReplacesResponseDataWithImpulseStub()
    {
        var app = BuildApp();
        AudysseyOnePostProcess.Apply(app);
        foreach (var ch in app.DetectedChannels)
        {
            foreach (var kv in ch.ResponseData)
            {
                Assert.Equal(16384, kv.Value.Length);
                Assert.Equal("1", kv.Value[0]);
                Assert.Equal("0", kv.Value[1]);
                Assert.Equal("0", kv.Value[16383]);
            }
        }
    }

    [Fact]
    public void Apply_SatelliteSnapsRolloffAndType()
    {
        var app = BuildApp();
        AudysseyOnePostProcess.Apply(app);
        var fl = app.DetectedChannels[0];
        Assert.False(fl.MidrangeCompensation);
        Assert.Equal(20000m, fl.FrequencyRangeRolloff);
        Assert.Equal("S", fl.CustomSpeakerType);
        Assert.Equal(1, fl.ChannelReport.EnSpeakerConnect);
    }

    [Fact]
    public void Apply_SubwooferSnapsRolloffAndZerosTrimDelay()
    {
        var app = BuildApp();
        AudysseyOnePostProcess.Apply(app);
        var sw = app.DetectedChannels[1];
        Assert.Equal(250m, sw.FrequencyRangeRolloff);
        Assert.Equal("0", sw.TrimAdjustment);
        Assert.Equal("0", sw.DelayAdjustment);
    }

    [Fact]
    public void Apply_NullApp_DoesNotThrow()
    {
        AudysseyOnePostProcess.Apply(null);
    }
}
