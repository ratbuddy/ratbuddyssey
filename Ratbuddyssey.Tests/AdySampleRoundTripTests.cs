#nullable disable
using System.Collections.Generic;
using System.IO;
using Audyssey.MultEQApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Ratbuddyssey.Tests;

public class AdySampleRoundTripTests
{
    private static readonly string SamplePath = Path.Combine("sample_ady", "tv36ipal v1.ady");

    private static readonly JsonSerializerSettings WriteSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
    };

    private static readonly JsonSerializerSettings ReadSettings = new()
    {
        FloatParseHandling = FloatParseHandling.Decimal,
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 64,
    };

    [Fact]
    public void Sample_RoundTrip_PreservesEveryFieldAndValue()
    {
        Assert.True(File.Exists(SamplePath), $"Sample file missing at {SamplePath}");
        string original = File.ReadAllText(SamplePath);

        var parsed = JsonConvert.DeserializeObject<AudysseyMultEQApp>(original, ReadSettings);
        Assert.NotNull(parsed);

        string roundTripped = JsonConvert.SerializeObject(parsed, WriteSettings);

        var beforeTok = JToken.Parse(original);
        var afterTok = JToken.Parse(roundTripped);

        // Compare structurally, ignoring property order. Any dropped key (e.g.
        // referenceCurveFilter pre-fix) or coerced type (string->number) will
        // surface here with a field-by-field diff in the failure message.
        var diff = FindFirstDifference(beforeTok, afterTok, "$");
        Assert.True(diff is null, diff);
    }

    [Fact]
    public void Sample_PreservesReferenceCurveFilterPerChannel()
    {
        string original = File.ReadAllText(SamplePath);
        var parsed = JsonConvert.DeserializeObject<AudysseyMultEQApp>(original, ReadSettings);

        Assert.NotNull(parsed);
        Assert.NotNull(parsed.DetectedChannels);
        Assert.NotEmpty(parsed.DetectedChannels);
        foreach (var ch in parsed.DetectedChannels)
        {
            Assert.NotNull(ch.ReferenceCurveFilter);
        }
    }

    [Fact]
    public void NumericLevelsAcceptedAsNumberOnRead()
    {
        // AudysseyOne writes customLevel/trimAdjustment/delayAdjustment as JSON
        // numbers. Verify our converter accepts that and re-emits as strings.
        const string Input = """
            {
              "title": "n",
              "detectedChannels": [
                {
                  "commandId": "FL",
                  "customLevel": -1.5,
                  "trimAdjustment": 0.5,
                  "delayAdjustment": 1
                }
              ]
            }
            """;

        var parsed = JsonConvert.DeserializeObject<AudysseyMultEQApp>(Input, ReadSettings);
        Assert.NotNull(parsed);
        var ch = parsed.DetectedChannels[0];
        Assert.Equal("-1.5", ch.CustomLevel);
        Assert.Equal("0.5", ch.TrimAdjustment);
        Assert.Equal("1", ch.DelayAdjustment);

        string output = JsonConvert.SerializeObject(parsed, WriteSettings);
        var token = JObject.Parse(output);
        var first = (JObject)token["detectedChannels"][0];
        Assert.Equal(JTokenType.String, first["customLevel"].Type);
        Assert.Equal(JTokenType.String, first["trimAdjustment"].Type);
        Assert.Equal(JTokenType.String, first["delayAdjustment"].Type);
    }

    /// <summary>
    /// Recursively diffs two JSON trees ignoring property ordering. Returns
    /// null on equality, otherwise a human-readable JSONPath-style description
    /// of the first divergence.
    /// </summary>
    private static string FindFirstDifference(JToken a, JToken b, string path)
    {
        if (a.Type != b.Type)
        {
            // Allow numeric tokens that came in as Float to match Integer if values are equal.
            if (IsNumeric(a) && IsNumeric(b) && (decimal)a == (decimal)b) return null;
            return $"{path}: type {a.Type} vs {b.Type} (values: {a} | {b})";
        }

        switch (a.Type)
        {
            case JTokenType.Object:
                var aObj = (JObject)a;
                var bObj = (JObject)b;
                var aKeys = new HashSet<string>();
                foreach (var prop in aObj.Properties()) aKeys.Add(prop.Name);
                foreach (var prop in bObj.Properties())
                {
                    if (!aKeys.Contains(prop.Name)) return $"{path}: extra key '{prop.Name}' in roundtrip";
                }
                foreach (var prop in aObj.Properties())
                {
                    var bProp = bObj.Property(prop.Name);
                    if (bProp == null) return $"{path}: key '{prop.Name}' dropped in roundtrip";
                    var d = FindFirstDifference(prop.Value, bProp.Value, $"{path}.{prop.Name}");
                    if (d != null) return d;
                }
                return null;
            case JTokenType.Array:
                var aArr = (JArray)a;
                var bArr = (JArray)b;
                if (aArr.Count != bArr.Count) return $"{path}: array length {aArr.Count} vs {bArr.Count}";
                for (int i = 0; i < aArr.Count; i++)
                {
                    var d = FindFirstDifference(aArr[i], bArr[i], $"{path}[{i}]");
                    if (d != null) return d;
                }
                return null;
            case JTokenType.Integer:
            case JTokenType.Float:
                return (decimal)a == (decimal)b ? null : $"{path}: {a} vs {b}";
            case JTokenType.Null:
                return null;
            default:
                return JToken.DeepEquals(a, b) ? null : $"{path}: {a} vs {b}";
        }
    }

    private static bool IsNumeric(JToken t) => t.Type == JTokenType.Integer || t.Type == JTokenType.Float;
}
