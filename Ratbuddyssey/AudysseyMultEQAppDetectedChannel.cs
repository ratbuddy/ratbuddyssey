using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Audyssey.MultEQ;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace Audyssey.MultEQApp;

[INotifyPropertyChanged]
public partial class DetectedChannel : MultEQList
{
    [ObservableProperty]
    private int? _enChannelType;

    [ObservableProperty]
    private bool? _isSkipMeasurement;

    [ObservableProperty]
    private string _delayAdjustment;

    [ObservableProperty]
    private string _commandId;

    [ObservableProperty]
    private string _trimAdjustment;

    [ObservableProperty]
    private ChannelReport _channelReport;

    [ObservableProperty]
    private Dictionary<string, string[]> _responseData;

    [ObservableProperty]
    private bool? _midrangeCompensation;

    [ObservableProperty]
    private decimal? _frequencyRangeRolloff;

    [ObservableProperty]
    private string _customLevel;

    [ObservableProperty]
    private string _customSpeakerType;

    [ObservableProperty]
    private decimal? _customDistance;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<MyKeyValuePair> _customTargetCurvePointsDictionary = new();

    [JsonIgnore]
    public bool Sticky { get; set; }

    private string _customCrossover;
    public string CustomCrossover
    {
        get => _customCrossover;
        set
        {
            if (SetProperty(ref _customCrossover, value))
            {
                _customCrossoverIndex = CrossoverList.IndexOf(value + "0");
                OnPropertyChanged(nameof(CustomCrossoverIndex));
            }
        }
    }

    private int _customCrossoverIndex = -1;
    [JsonIgnore]
    public int CustomCrossoverIndex
    {
        get => _customCrossoverIndex;
        set
        {
            if (SetProperty(ref _customCrossoverIndex, value))
            {
                _customCrossover = CrossoverList[value];
                OnPropertyChanged(nameof(CustomCrossover));
            }
        }
    }

    public string[] CustomTargetCurvePoints
    {
        get => ConvertDictionaryToStringArray(CustomTargetCurvePointsDictionary);
        set
        {
            CustomTargetCurvePointsDictionary = ConvertStringArrayToDictionary(value);
            OnPropertyChanged(nameof(CustomTargetCurvePoints));
        }
    }

    // Newtonsoft.Json's ShouldSerializeXxx convention requires instance methods, even when the body
    // does not touch instance state. Suppress CA1822 ("can be marked static") accordingly.
#pragma warning disable CA1822
    public bool ShouldSerializeCustomTargetCurvePoints() => EnChannelType != 55;
    public bool ShouldSerializeCustomSpeakerType() => CustomSpeakerType != null;
    public bool ShouldSerializeCustomDistance() => CustomDistance.HasValue;
    public bool ShouldSerializeCustomCrossover() => CustomCrossover != null;
#pragma warning restore CA1822

    private static ObservableCollection<MyKeyValuePair> ConvertStringArrayToDictionary(string[] array)
    {
        var result = new ObservableCollection<MyKeyValuePair>();
        foreach (string s in array)
        {
            string str = s.Substring(1, s.Length - 2);
            string[] arr = str.Split(',');
            result.Add(new MyKeyValuePair(arr[0], arr[1]));
        }
        return new ObservableCollection<MyKeyValuePair>(result.OrderBy(x => double.Parse(x.Key, CultureInfo.InvariantCulture)));
    }

    private static string[] ConvertDictionaryToStringArray(ObservableCollection<MyKeyValuePair> dict)
    {
        var result = new string[dict.Count];
        for (int i = 0; i < dict.Count; i++)
        {
            result[i] = dict[i].ToString();
        }
        return result;
    }

    public override string ToString()
    {
        return $"DetectedChannel(EnChannelType={EnChannelType}, ResponseDataKeys={ResponseData?.Count ?? 0})";
    }
}

public partial class MyKeyValuePair : ObservableObject
{
    private const double KeyMin = 10;       // 10 Hz - Chris Kyriakakis
    private const double KeyMax = 24000;    // 24000 Hz - Chris Kyriakakis
    private const double ValueMin = -20;    // -12 dB Audyssey app -> -20 dB Chris Kyriakakis
    private const double ValueMax = 12;     // +9 dB Chris Kyriakakis -> +12 dB observed in .ady files

    private string _key;
    public string Key
    {
        get => _key;
        set
        {
            double d = double.Parse(value, CultureInfo.InvariantCulture);
            if (d >= KeyMin && d <= KeyMax)
            {
                SetProperty(ref _key, value);
            }
        }
    }

    private string _value;
    public string Value
    {
        get => _value;
        set
        {
            double d = double.Parse(value, CultureInfo.InvariantCulture);
            if (d >= ValueMin && d <= ValueMax)
            {
                SetProperty(ref _value, value);
            }
        }
    }

    public MyKeyValuePair(string key, string value)
    {
        Key = key.Trim();
        Value = value.Trim();
    }

    public MyKeyValuePair(decimal key, decimal value)
    {
        Key = key.ToString(CultureInfo.InvariantCulture);
        Value = value.ToString(CultureInfo.InvariantCulture);
    }

    public override string ToString() => "{" + Key + ", " + Value + "}";
}
