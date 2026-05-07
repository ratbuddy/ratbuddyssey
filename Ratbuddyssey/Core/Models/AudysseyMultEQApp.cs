using System.Collections.ObjectModel;
using System.Text;
using Audyssey.MultEQ;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace Audyssey.MultEQApp;

[INotifyPropertyChanged]
public partial class AudysseyMultEQApp : MultEQList
{
    [ObservableProperty]
    [property: JsonProperty(Order = 1)]
    private string _title;

    [ObservableProperty]
    [property: JsonProperty(Order = 2)]
    private string _targetModelName;

    [ObservableProperty]
    [property: JsonProperty(Order = 3)]
    private string _interfaceVersion;

    [ObservableProperty]
    [property: JsonProperty(Order = 4)]
    private bool? _dynamicEq;

    [ObservableProperty]
    [property: JsonProperty(Order = 5)]
    private bool? _dynamicVolume;

    [ObservableProperty]
    [property: JsonProperty(Order = 6)]
    private bool? _lfcSupport;

    [ObservableProperty]
    [property: JsonProperty(Order = 7)]
    private bool? _lfc;

    [ObservableProperty]
    [property: JsonProperty(Order = 8)]
    private int? _systemDelay;

    [ObservableProperty]
    [property: JsonProperty(Order = 9)]
    private decimal? _adcLineup;

    [ObservableProperty]
    [property: JsonProperty(Order = 10)]
    private int? _enTargetCurveType;

    [ObservableProperty]
    [property: JsonProperty(Order = 11)]
    private int? _enAmpAssignType;

    [ObservableProperty]
    [property: JsonProperty(Order = 12)]
    private int? _enMultEQType;

    [ObservableProperty]
    [property: JsonProperty(Order = 13)]
    private string _ampAssignInfo;

    [ObservableProperty]
    [property: JsonProperty(Order = 14)]
    private bool? _auro;

    [ObservableProperty]
    [property: JsonProperty(Order = 15)]
    private string _upgradeInfo;

    [ObservableProperty]
    [property: JsonProperty(Order = 16)]
    private ObservableCollection<DetectedChannel> _detectedChannels;

    [JsonIgnore]
    public string TargetCurveType
    {
        get => GetListItem(TargetCurveTypeList, EnTargetCurveType);
        set
        {
            int idx = TargetCurveTypeList.IndexOf(value);
            EnTargetCurveType = idx >= 0 ? idx : (int?)null;
            OnPropertyChanged(nameof(TargetCurveType));
        }
    }

    [JsonIgnore]
    public string AmpAssignType
    {
        get => GetListItem(AmpAssignTypeList, EnAmpAssignType);
        set
        {
            int idx = AmpAssignTypeList.IndexOf(value);
            EnAmpAssignType = idx >= 0 ? idx : (int?)null;
            OnPropertyChanged(nameof(AmpAssignType));
        }
    }

    [JsonIgnore]
    public string MultEQType
    {
        get => GetListItem(MultEQTypeList, EnMultEQType);
        set
        {
            int idx = MultEQTypeList.IndexOf(value);
            EnMultEQType = idx >= 0 ? idx : (int?)null;
            OnPropertyChanged(nameof(MultEQType));
        }
    }

    /// <summary>
    /// Safely resolves an enum-as-int (nullable, may be out of range from a
    /// future schema bump or a corrupt file) to a list entry. Returns null on
    /// any out-of-band index so bindings render "no selection" instead of
    /// throwing inside a debugger / data-template evaluation.
    /// </summary>
    private static string GetListItem(System.Collections.ObjectModel.ObservableCollection<string> list, int? index)
    {
        if (list == null || index == null) return null;
        int i = index.Value;
        return (i >= 0 && i < list.Count) ? list[i] : null;
    }
}
