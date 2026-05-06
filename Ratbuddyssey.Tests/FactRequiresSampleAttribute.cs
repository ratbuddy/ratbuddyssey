using System.IO;
using Xunit;

namespace Ratbuddyssey.Tests;

/// <summary>
/// xUnit fact that skips automatically when the gitignored sample
/// <c>sample_ady/tv36ipal v1.ady</c> is not present in the test output
/// directory (e.g. CI, fresh clones). Locally — once the sample is in place
/// — the test runs normally.
/// </summary>
internal sealed class FactRequiresSampleAttribute : FactAttribute
{
    private static readonly string SamplePath =
        Path.Combine("sample_ady", "tv36ipal v1.ady");

    public FactRequiresSampleAttribute()
    {
        if (!File.Exists(SamplePath))
        {
            Skip = $"Sample .ady not present at '{SamplePath}' (gitignored).";
        }
    }
}
