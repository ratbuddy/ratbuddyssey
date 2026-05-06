#nullable disable
#pragma warning disable CA1305, CA1310 // Test-only string ops; invariant culture not material here.
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Audyssey.MultEQApp;
using Ratbuddyssey.Audio.Analysis;
using Xunit;

namespace Ratbuddyssey.Tests;

public class CalibrationAnalyzerTests
{
    private static AudysseyMultEQApp Cal(params DetectedChannel[] chs) =>
        new AudysseyMultEQApp { DetectedChannels = new ObservableCollection<DetectedChannel>(chs) };

    private static DetectedChannel Ch(string id, string trim = null, string xo = null) =>
        new DetectedChannel { CommandId = id, TrimAdjustment = trim, CustomCrossover = xo };

    [Fact]
    public void Analyze_NullCalibration_ReturnsEmpty()
    {
        Assert.Empty(new CalibrationAnalyzer().Analyze(null));
    }

    [Fact]
    public void Analyze_NoChannels_ReturnsEmpty()
    {
        Assert.Empty(new CalibrationAnalyzer().Analyze(new AudysseyMultEQApp()));
    }

    [Fact]
    public void TrimLimits_FiresAboveAndBelow()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(
            Ch("FL", "11"),
            Ch("FR", "-10.5"),
            Ch("C", "0")));
        Assert.Contains(w, x => x.Code == "trim.aboveLimit" && x.Channel == "FL");
        Assert.Contains(w, x => x.Code == "trim.belowLimit" && x.Channel == "FR");
        Assert.DoesNotContain(w, x => x.Channel == "C" && x.Code.StartsWith("trim."));
    }

    [Fact]
    public void TrimLimits_GarbageValueDoesNotCrashOrFire()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "not-a-number")));
        Assert.DoesNotContain(w, x => x.Code.StartsWith("trim."));
    }

    [Fact]
    public void FrontPair_FlagsLargerThan3dB()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "0"), Ch("FR", "4")));
        Assert.Contains(w, x => x.Code == "frontPair.trimImbalance");
    }

    [Fact]
    public void FrontPair_DoesNotFire_AtOrBelow3dB()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "1"), Ch("FR", "4")));
        Assert.DoesNotContain(w, x => x.Code == "frontPair.trimImbalance");
    }

    [Fact]
    public void FrontPair_MissingPair_DoesNotFire()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "0")));
        Assert.DoesNotContain(w, x => x.Code == "frontPair.trimImbalance");
    }

    [Fact]
    public void Crossovers_FlagsNonStandard()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "0", "200")));
        Assert.Contains(w, x => x.Code == "crossover.nonStandard" && x.Channel == "FL");
    }

    [Fact]
    public void Crossovers_StandardValuesDoNotFire()
    {
        var ids = new[] { 40, 60, 80, 90, 100, 110, 120, 150 };
        foreach (var hz in ids)
        {
            var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "0", hz.ToString())));
            Assert.DoesNotContain(w, x => x.Code == "crossover.nonStandard");
        }
    }

    [Fact]
    public void Crossovers_FullRangeIgnored()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "0", "F")));
        Assert.DoesNotContain(w, x => x.Code == "crossover.nonStandard");
    }

    [Fact]
    public void Subwoofer_NearCeilingFires()
    {
        var sub = Ch("SW1", "11");
        var w = new CalibrationAnalyzer().Analyze(Cal(sub));
        Assert.Contains(w, x => x.Code == "sub.trimNearCeiling" && x.Channel == "SW1");
    }

    [Fact]
    public void Subwoofer_MainsCrossoverLowFiresOnlyWhenSubPresent()
    {
        var withSub = new CalibrationAnalyzer().Analyze(
            Cal(Ch("FL", "0", "40"), Ch("SW1", "0")));
        Assert.Contains(withSub, x => x.Code == "sub.mainsCrossoverLow" && x.Channel == "FL");

        var withoutSub = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "0", "40")));
        Assert.DoesNotContain(withoutSub, x => x.Code == "sub.mainsCrossoverLow");
    }

    [Fact]
    public void Subwoofer_MainsCrossoverSpreadFiresOnLargeDelta()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(
            Ch("FL", "0", "40"),
            Ch("FR", "0", "40"),
            Ch("C", "0", "120"),
            Ch("SW1", "0")));
        Assert.Contains(w, x => x.Code == "sub.mainsCrossoverMismatch");
    }

    [Fact]
    public void NullChannelEntries_DoNotCrash()
    {
        var cal = new AudysseyMultEQApp
        {
            DetectedChannels = new ObservableCollection<DetectedChannel> { null, Ch("FL", "0") },
        };
        var w = new CalibrationAnalyzer().Analyze(cal);
        Assert.NotNull(w);
    }

    [Fact]
    public void LowFrequencyNull_NoResponseData_NoFinding()
    {
        var w = new CalibrationAnalyzer().Analyze(Cal(Ch("FL", "0")));
        Assert.DoesNotContain(w, x => x.Code == "lowFreq.null");
    }

    [Fact]
    public void Warning_ToString_IncludesSeverityAndCode()
    {
        var w = new CalibrationWarning("trim.aboveLimit", CalibrationWarningSeverity.Warning, "FL", "Trim too hot.");
        Assert.Contains("Warning", w.ToString());
        Assert.Contains("trim.aboveLimit", w.ToString());
        Assert.Contains("FL", w.ToString());
    }
}
