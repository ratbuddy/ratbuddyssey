using Audyssey.MultEQApp;
using Xunit;

namespace Ratbuddyssey.Tests;

public class MyKeyValuePairTests
{
    [Fact]
    public void Constructor_AcceptsInRangeValues()
    {
        var p = new MyKeyValuePair("100", "0.5");
        Assert.Equal("100", p.Key);
        Assert.Equal("0.5", p.Value);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData("5")]      // below KeyMin (10 Hz)
    [InlineData("30000")]  // above KeyMax (24000 Hz)
    public void Key_RejectsInvalidInputWithoutThrowing(string bad)
    {
        var p = new MyKeyValuePair("100", "0.5");
        p.Key = bad;
        Assert.Equal("100", p.Key);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("-50")]   // below ValueMin
    [InlineData("100")]   // above ValueMax
    public void Value_RejectsInvalidInputWithoutThrowing(string bad)
    {
        var p = new MyKeyValuePair("100", "0.5");
        p.Value = bad;
        Assert.Equal("0.5", p.Value);
    }

    [Fact]
    public void Constructor_RejectsGarbageAndLeavesKeyValueNull()
    {
        var p = new MyKeyValuePair("garbage", "garbage");
        Assert.Null(p.Key);
        Assert.Null(p.Value);
    }
}
