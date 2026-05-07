#nullable disable
using System.Collections.Generic;
using System.Linq;
using Audyssey.MultEQApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Ratbuddyssey.Tests;

/// <summary>
/// Drift detector: serialize freshly-constructed model objects and assert
/// the JSON key set matches the expected schema. Catches three failure modes:
/// adding a property without thinking about wire impact, renaming one, or
/// accidentally [JsonIgnore]-ing one that the official MultEQ Editor needs.
/// </summary>
public class JsonSchemaCoverageTests
{
    private static readonly JsonSerializerSettings WriteSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Include,
    };

    [Fact]
    public void AudysseyMultEQApp_DefaultJsonKeys_MatchSchema()
    {
        var expected = new[]
        {
            "title", "targetModelName", "interfaceVersion",
            "dynamicEq", "dynamicVolume", "lfcSupport", "lfc",
            "systemDelay", "adcLineup",
            "enTargetCurveType", "enAmpAssignType", "enMultEQType",
            "ampAssignInfo", "auro", "upgradeInfo",
            "detectedChannels",
        };

        var json = JsonConvert.SerializeObject(new AudysseyMultEQApp(), WriteSettings);
        var actual = JObject.Parse(json).Properties().Select(p => p.Name).ToArray();

        AssertSetEqual(expected, actual, "AudysseyMultEQApp");
    }

    [Fact]
    public void AudysseyMultEQApp_KeysAppearInAudysseyExpectedOrder()
    {
        // Audyssey MultEQ Editor parses positionally for some fields. Order
        // is enforced via [JsonProperty(Order = N)]; verify it didn't drift.
        var expected = new[]
        {
            "title", "targetModelName", "interfaceVersion",
            "dynamicEq", "dynamicVolume", "lfcSupport", "lfc",
            "systemDelay", "adcLineup",
            "enTargetCurveType", "enAmpAssignType", "enMultEQType",
            "ampAssignInfo", "auro", "upgradeInfo",
            "detectedChannels",
        };

        var json = JsonConvert.SerializeObject(new AudysseyMultEQApp(), WriteSettings);
        var actual = JObject.Parse(json).Properties().Select(p => p.Name).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DetectedChannel_DefaultJsonKeys_MatchSchema()
    {
        // Note: a default DetectedChannel has many ShouldSerializeXxx guards
        // that suppress null-valued fields. We pre-fill so every key surfaces.
        var ch = new DetectedChannel
        {
            EnChannelType = 0,
            IsSkipMeasurement = false,
            CommandId = "FL",
            DelayAdjustment = "0",
            TrimAdjustment = "0",
            CustomLevel = "0",
            CustomDistance = 0m,
            CustomCrossover = "80",
            CustomSpeakerType = "S",
            FrequencyRangeRolloff = 0m,
            MidrangeCompensation = false,
            ChannelReport = new ChannelReport(),
            ReferenceCurveFilter = new JObject(),
            ResponseData = new System.Collections.Generic.Dictionary<string, string[]>(),
        };

        var expected = new[]
        {
            "enChannelType", "isSkipMeasurement", "delayAdjustment", "commandId",
            "trimAdjustment", "channelReport", "referenceCurveFilter", "responseData",
            "midrangeCompensation", "frequencyRangeRolloff",
            "customLevel", "customSpeakerType", "customDistance",
            "customCrossover", "customTargetCurvePoints",
        };

        var json = JsonConvert.SerializeObject(ch, WriteSettings);
        var actual = JObject.Parse(json).Properties().Select(p => p.Name).ToArray();

        AssertSetEqual(expected, actual, "DetectedChannel");
    }

    [Fact]
    public void ChannelReport_DefaultJsonKeys_MatchSchema()
    {
        var report = new ChannelReport
        {
            EnSpeakerConnect = 0,
            IsReversePolarity = false,
            Distance = 0m,
        };
        var expected = new[]
        {
            "enSpeakerConnect", "isReversePolarity", "distance",
        };

        var json = JsonConvert.SerializeObject(report, WriteSettings);
        var actual = JObject.Parse(json).Properties().Select(p => p.Name).ToArray();

        AssertSetEqual(expected, actual, "ChannelReport");
    }

    private static void AssertSetEqual(IEnumerable<string> expected, IEnumerable<string> actual, string label)
    {
        var exp = new HashSet<string>(expected);
        var act = new HashSet<string>(actual);
        var missing = exp.Except(act).ToArray();
        var extra = act.Except(exp).ToArray();
        Assert.True(missing.Length == 0 && extra.Length == 0,
            $"{label}: missing=[{string.Join(",", missing)}] extra=[{string.Join(",", extra)}]");
    }
}
