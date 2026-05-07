using System.Linq;
using Audyssey.MultEQApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Ratbuddyssey.Tests;

public class AudysseyAppRoundTripTests
{
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
    public void Serialize_PreservesAudysseyPropertyOrder()
    {
        // The official Audyssey app expects keys in a specific order; verify the
        // [JsonProperty(Order = N)] attributes still drive serialization order.
        var app = new AudysseyMultEQApp
        {
            Title = "test",
            TargetModelName = "model",
            InterfaceVersion = "1.0",
            DynamicEq = true,
            DynamicVolume = false,
            EnTargetCurveType = 0,
            EnAmpAssignType = 0,
            EnMultEQType = 0,
            AmpAssignInfo = "info",
        };

        var json = JsonConvert.SerializeObject(app, WriteSettings);
        var keys = JObject.Parse(json).Properties().Select(p => p.Name).ToArray();

        var titleIdx = System.Array.IndexOf(keys, "title");
        var modelIdx = System.Array.IndexOf(keys, "targetModelName");
        var ifaceIdx = System.Array.IndexOf(keys, "interfaceVersion");
        var ampInfoIdx = System.Array.IndexOf(keys, "ampAssignInfo");
        var detectedIdx = System.Array.IndexOf(keys, "detectedChannels");

        Assert.True(titleIdx >= 0 && modelIdx > titleIdx && ifaceIdx > modelIdx,
            $"Expected title < targetModelName < interfaceVersion; got: {string.Join(",", keys)}");
        Assert.True(ampInfoIdx > ifaceIdx,
            $"Expected ampAssignInfo after interfaceVersion; got: {string.Join(",", keys)}");
        Assert.True(detectedIdx == -1 || detectedIdx > ampInfoIdx,
            "Expected detectedChannels (when present) at or near the end.");
    }

    [Fact]
    public void RoundTrip_PreservesScalarFields()
    {
        var original = new AudysseyMultEQApp
        {
            Title = "round-trip",
            TargetModelName = "AVR-X",
            InterfaceVersion = "2.0.1",
            DynamicEq = true,
            DynamicVolume = false,
            LfcSupport = true,
            Lfc = false,
            SystemDelay = 12,
            AdcLineup = 0.5m,
            EnTargetCurveType = 1,
            EnAmpAssignType = 0,
            EnMultEQType = 2,
            AmpAssignInfo = "5.1",
            UpgradeInfo = "none",
        };

        var json = JsonConvert.SerializeObject(original, WriteSettings);
        var restored = JsonConvert.DeserializeObject<AudysseyMultEQApp>(json, ReadSettings);

        Assert.NotNull(restored);
        Assert.Equal(original.Title, restored.Title);
        Assert.Equal(original.TargetModelName, restored.TargetModelName);
        Assert.Equal(original.InterfaceVersion, restored.InterfaceVersion);
        Assert.Equal(original.DynamicEq, restored.DynamicEq);
        Assert.Equal(original.DynamicVolume, restored.DynamicVolume);
        Assert.Equal(original.LfcSupport, restored.LfcSupport);
        Assert.Equal(original.Lfc, restored.Lfc);
        Assert.Equal(original.SystemDelay, restored.SystemDelay);
        Assert.Equal(original.AdcLineup, restored.AdcLineup);
        Assert.Equal(original.EnTargetCurveType, restored.EnTargetCurveType);
        Assert.Equal(original.EnAmpAssignType, restored.EnAmpAssignType);
        Assert.Equal(original.EnMultEQType, restored.EnMultEQType);
        Assert.Equal(original.AmpAssignInfo, restored.AmpAssignInfo);
        Assert.Equal(original.UpgradeInfo, restored.UpgradeInfo);
    }

    [Fact]
    public void ObservableProperty_RaisesPropertyChanged()
    {
        var app = new AudysseyMultEQApp();
        string? lastChanged = null;
        app.PropertyChanged += (_, e) => lastChanged = e.PropertyName;

        app.Title = "new title";

        Assert.Equal(nameof(AudysseyMultEQApp.Title), lastChanged);
    }
}
