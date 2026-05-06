#nullable disable
#pragma warning disable CA1861 // Constant arrays inline keep tests readable.
using System.Collections.Generic;
using Audyssey.MultEQApp;
using Ratbuddyssey;
using Xunit;

namespace Ratbuddyssey.Tests;

public class ResponseAveragingTests
{
    [Fact]
    public void AverageResponses_NullEnumerable_ReturnsEmpty()
    {
        var avg = ResponseAveraging.AverageResponses((IEnumerable<double[]>)null);
        Assert.Empty(avg);
    }

    [Fact]
    public void AverageResponses_AllNullOrEmpty_ReturnsEmpty()
    {
        var avg = ResponseAveraging.AverageResponses(new double[][]
        {
            null,
            System.Array.Empty<double>(),
            null,
        });
        Assert.Empty(avg);
    }

    [Fact]
    public void AverageResponses_SameLength_ProducesElementwiseMean()
    {
        var avg = ResponseAveraging.AverageResponses(new[]
        {
            new[] { 1.0, 2.0, 3.0, 4.0 },
            new[] { 3.0, 4.0, 5.0, 6.0 },
        });
        Assert.Equal(new[] { 2.0, 3.0, 4.0, 5.0 }, avg);
    }

    [Fact]
    public void AverageResponses_MismatchedLengths_TruncatesToShortest()
    {
        var avg = ResponseAveraging.AverageResponses(new[]
        {
            new[] { 1.0, 2.0, 3.0, 4.0, 5.0 },
            new[] { 3.0, 4.0, 5.0 },
            new[] { 5.0, 6.0, 7.0, 8.0 },
        });
        Assert.Equal(3, avg.Length);
        Assert.Equal(3.0, avg[0]); // (1+3+5)/3
        Assert.Equal(4.0, avg[1]); // (2+4+6)/3
        Assert.Equal(5.0, avg[2]); // (3+5+7)/3
    }

    [Fact]
    public void AverageResponses_StringOverload_HandlesMalformedTokens()
    {
        var avg = ResponseAveraging.AverageResponses(new[]
        {
            new[] { "1.0", "garbage", "3.0" },
            new[] { "3.0", "4.0", "5.0" },
        });
        Assert.Equal(3, avg.Length);
        Assert.Equal(2.0, avg[0]);
        Assert.Equal(2.0, avg[1]); // (0 + 4) / 2
        Assert.Equal(4.0, avg[2]);
    }

    [Fact]
    public void GetAveragedChannelResponse_NullChannel_ReturnsEmpty()
    {
        Assert.Empty(ResponseAveraging.GetAveragedChannelResponse(null));
    }

    [Fact]
    public void GetAveragedChannelResponse_NoResponseData_ReturnsEmpty()
    {
        var ch = new DetectedChannel { CommandId = "FL" };
        Assert.Empty(ResponseAveraging.GetAveragedChannelResponse(ch));
    }

    [Fact]
    public void GetAveragedChannelResponse_MultiplePositions_AveragesAll()
    {
        var ch = new DetectedChannel
        {
            CommandId = "FL",
            ResponseData = new Dictionary<string, string[]>
            {
                ["0"] = new[] { "1", "2", "3" },
                ["1"] = new[] { "3", "4", "5" },
                ["2"] = null,                    // ignored
                ["3"] = System.Array.Empty<string>(),  // ignored
            },
        };
        var avg = ResponseAveraging.GetAveragedChannelResponse(ch);
        Assert.Equal(new[] { 2.0, 3.0, 4.0 }, avg);
    }

    [Fact]
    public void GetChannelSamples_DefaultsToSingleMicPosition()
    {
        var ch = new DetectedChannel
        {
            ResponseData = new Dictionary<string, string[]>
            {
                ["0"] = new[] { "1.0", "2.0" },
                ["1"] = new[] { "9.0", "9.0" },
            },
        };
        var s = ChartDataPrep.GetChannelSamples(ch, "0");
        Assert.Equal(new[] { "1.0", "2.0" }, s);
    }

    [Fact]
    public void GetChannelSamples_AveragedSource_ReturnsAveragedSamples()
    {
        var ch = new DetectedChannel
        {
            CommandId = "FL",
            ResponseData = new Dictionary<string, string[]>
            {
                ["0"] = new[] { "1", "2", "3" },
                ["1"] = new[] { "3", "4", "5" },
            },
        };
        var s = ChartDataPrep.GetChannelSamples(ch, micPositionKey: null,
            source: ChannelResponseSource.AveragedAcrossMicPositions);
        Assert.Equal(3, s.Length);
        Assert.Equal(2.0, ChartDataPrep.TryParseDouble(s[0]));
        Assert.Equal(3.0, ChartDataPrep.TryParseDouble(s[1]));
        Assert.Equal(4.0, ChartDataPrep.TryParseDouble(s[2]));
    }

    [Fact]
    public void GetChannelSamples_MissingKey_ReturnsEmpty()
    {
        var ch = new DetectedChannel
        {
            ResponseData = new Dictionary<string, string[]> { ["0"] = new[] { "1" } },
        };
        Assert.Empty(ChartDataPrep.GetChannelSamples(ch, "9"));
        Assert.Empty(ChartDataPrep.GetChannelSamples(null, "0"));
    }
}
