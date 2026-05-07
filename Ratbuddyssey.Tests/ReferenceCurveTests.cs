using System.IO;
using Audyssey;
using Xunit;

namespace Ratbuddyssey.Tests;

public class ReferenceCurveTests
{
    private static string CurvePath(string name)
        => Path.Combine(AppContext.BaseDirectory, name);

    [Theory]
    [InlineData("high_frequency_roll_off_1.json")]
    [InlineData("high_frequency_roll_off_2.json")]
    public void ReadPointsFromJsonFile_ReturnsMonotonicIncreasingX(string fileName)
    {
        var path = CurvePath(fileName);
        Assert.True(File.Exists(path), $"Expected reference curve copied to test output: {path}");

        var points = AudysseyMultEQReferenceCurveFilter.ReadPointsFromJsonFile(path);

        Assert.NotNull(points);
        Assert.NotEmpty(points);
        for (int i = 1; i < points.Count; i++)
        {
            Assert.True(points[i].X > points[i - 1].X,
                $"X values must be strictly increasing for interpolation; failed at index {i}.");
        }
    }

    [Fact]
    public void ReadPointsFromJsonFile_MissingFile_ReturnsNull()
    {
        var result = AudysseyMultEQReferenceCurveFilter.ReadPointsFromJsonFile(
            Path.Combine(AppContext.BaseDirectory, "does_not_exist.json"));
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_LoadsBothShippedCurves()
    {
        var filter = new AudysseyMultEQReferenceCurveFilter();
        Assert.NotNull(filter.HighFrequencyRollOff1());
        Assert.NotNull(filter.HighFrequencyRollOff2());
        Assert.NotEmpty(filter.HighFrequencyRollOff1());
        Assert.NotEmpty(filter.HighFrequencyRollOff2());
    }
}
