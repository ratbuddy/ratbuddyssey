#nullable disable
using Audyssey;
using Audyssey.MultEQApp;
using Xunit;

namespace Ratbuddyssey.Tests;

public class ChannelLimitsValidatorTests
{
    [Fact]
    public void Subwoofer_LevelBelowFloor_ReturnsError()
    {
        var ch = new DetectedChannel { CommandId = "SW1", CustomLevel = "-15" };
        var v = ChannelLimitsValidator.Validate(ch, subwooferTrimFloorDb: -8m);
        Assert.Equal(ValidationSeverity.Error, v.Severity);
    }

    [Fact]
    public void Satellite_LevelAtBoundary_IsOk()
    {
        var ch = new DetectedChannel { CommandId = "FL", CustomLevel = "12" };
        var v = ChannelLimitsValidator.Validate(ch, subwooferTrimFloorDb: -12m);
        Assert.Equal(ValidationSeverity.Ok, v.Severity);
    }

    [Fact]
    public void Satellite_LevelOver12dB_ReturnsError()
    {
        var ch = new DetectedChannel { CommandId = "FL", CustomLevel = "12.5" };
        var v = ChannelLimitsValidator.Validate(ch, subwooferTrimFloorDb: -12m);
        Assert.Equal(ValidationSeverity.Error, v.Severity);
    }

    [Fact]
    public void Crossover_NotInSnapList_ReturnsWarning()
    {
        var ch = new DetectedChannel { CommandId = "FL", CustomLevel = "0", CustomCrossover = "75" };
        var v = ChannelLimitsValidator.Validate(ch, subwooferTrimFloorDb: -12m);
        Assert.Equal(ValidationSeverity.Warning, v.Severity);
    }

    [Fact]
    public void Crossover_FullRange_F_IsAccepted()
    {
        var ch = new DetectedChannel { CommandId = "FL", CustomLevel = "0", CustomCrossover = "F" };
        var v = ChannelLimitsValidator.Validate(ch, subwooferTrimFloorDb: -12m);
        Assert.Equal(ValidationSeverity.Ok, v.Severity);
    }

    [Fact]
    public void Crossover_OnSnapValue_IsOk()
    {
        var ch = new DetectedChannel { CommandId = "C", CustomLevel = "0", CustomCrossover = "80" };
        var v = ChannelLimitsValidator.Validate(ch, subwooferTrimFloorDb: -12m);
        Assert.Equal(ValidationSeverity.Ok, v.Severity);
    }

    [Fact]
    public void NullChannel_IsOk()
    {
        var v = ChannelLimitsValidator.Validate(null, subwooferTrimFloorDb: -12m);
        Assert.Equal(ValidationSeverity.Ok, v.Severity);
    }
}

public class AudysseyChannelNamesTests
{
    [Theory]
    [InlineData("FL", "Front L")]
    [InlineData("FR", "Front R")]
    [InlineData("C", "Center")]
    [InlineData("SLA", "Surround L")]
    [InlineData("SRA", "Surround R")]
    [InlineData("TFL", "Top Front L")]
    [InlineData("TRR", "Top Rear R")]
    [InlineData("SW1", "Subwoofer 1")]
    [InlineData("SW2", "Subwoofer 2")]
    [InlineData("SW", "Subwoofer")]
    [InlineData("XYZ", "XYZ")]
    [InlineData("", "")]
    public void Friendly_MapsKnownTokens(string id, string expected)
    {
        Assert.Equal(expected, AudysseyChannelNames.Friendly(id));
    }
}
