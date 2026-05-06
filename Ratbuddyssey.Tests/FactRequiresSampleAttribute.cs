using System;
using System.IO;
using Xunit;

namespace Ratbuddyssey.Tests;

/// <summary>
/// xUnit fact that skips automatically when the gitignored sample
/// <c>sample_ady/tv36ipal v1.ady</c> is not present beside the test binary
/// (e.g. CI, fresh clones). Locally — once the sample is in place — the test
/// runs normally.
/// </summary>
internal sealed class FactRequiresSampleAttribute : FactAttribute
{
    // Anchor to the test binary's directory rather than
    // Environment.CurrentDirectory: VS Test, dotnet test --results-directory,
    // and Rider all set cwd to varying locations, but the project file copies
    // the sample next to the assembly so AppContext.BaseDirectory always
    // resolves correctly.
    private static readonly string SamplePath =
        Path.Combine(AppContext.BaseDirectory, "sample_ady", "tv36ipal v1.ady");

    public FactRequiresSampleAttribute()
    {
        if (!File.Exists(SamplePath))
        {
            Skip = $"Sample .ady not present at '{SamplePath}' (gitignored).";
        }
    }
}
