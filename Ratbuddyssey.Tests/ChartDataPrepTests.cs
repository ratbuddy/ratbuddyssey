using Ratbuddyssey;
using Xunit;

namespace Ratbuddyssey.Tests;

public class ChartDataPrepTests
{
    [Fact]
    public void BuildChirpSeries_EmptyOrNull_ReturnsEmpty()
    {
        var (xs1, ys1) = ChartDataPrep.BuildChirpSeries(null);
        Assert.Empty(xs1);
        Assert.Empty(ys1);

        var (xs2, ys2) = ChartDataPrep.BuildChirpSeries(System.Array.Empty<string>());
        Assert.Empty(xs2);
        Assert.Empty(ys2);
    }

    [Fact]
    public void BuildChirpSeries_GoodSamples_ProducesMonotonicTimeAxis()
    {
        var samples = new[] { "0.0", "0.5", "-0.25", "1e-3" };
        var (xs, ys) = ChartDataPrep.BuildChirpSeries(samples);

        Assert.Equal(4, xs.Length);
        Assert.Equal(4, ys.Length);
        Assert.Equal(0.0, xs[0]);
        for (int i = 1; i < xs.Length; i++)
        {
            Assert.True(xs[i] > xs[i - 1], "time axis must be strictly increasing");
        }
        Assert.Equal(0.0, ys[0]);
        Assert.Equal(0.5, ys[1]);
        Assert.Equal(-0.25, ys[2]);
        Assert.Equal(0.001, ys[3]);
    }

    [Fact]
    public void BuildChirpSeries_GarbageSampleBecomesZero_DoesNotThrow()
    {
        var samples = new[] { "0.5", "not-a-number", "0.25" };
        var (_, ys) = ChartDataPrep.BuildChirpSeries(samples);
        Assert.Equal(0.5, ys[0]);
        Assert.Equal(0.0, ys[1]);
        Assert.Equal(0.25, ys[2]);
    }

    [Fact]
    public void BuildSpectrumInput_BuildsLinearFreqAxisFromSampleRate()
    {
        // For N samples, freq[k] = k/N * 48000.
        var samples = new[] { "1", "0", "-1", "0" };
        var (cValues, freqs) = ChartDataPrep.BuildSpectrumInput(samples);

        Assert.Equal(4, cValues.Length);
        Assert.Equal(4, freqs.Length);
        Assert.Equal(0.0, freqs[0]);
        Assert.Equal(48000.0 / 4, freqs[1]);
        Assert.Equal(48000.0 / 2, freqs[2]);

        // values are scaled by 100 in the complex domain
        Assert.Equal(100.0, cValues[0].Real);
        Assert.Equal(-100.0, cValues[2].Real);
    }

    [Fact]
    public void BuildSpectrumInput_GarbageBecomesZero_DoesNotThrow()
    {
        var samples = new[] { "garbage", "0.5" };
        var (cValues, _) = ChartDataPrep.BuildSpectrumInput(samples);
        Assert.Equal(0.0, cValues[0].Real);
        Assert.Equal(50.0, cValues[1].Real);
    }
}
