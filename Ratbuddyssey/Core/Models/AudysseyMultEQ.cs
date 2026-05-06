using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Audyssey.MultEQ;

public class MultEQList
{
    [JsonIgnore]
    public ObservableCollection<string> AmpAssignTypeList { get; } = new()
    {
        "FrontA", "FrontB", "Type3", "Type4",
        "Type5", "Type6", "Type7", "Type8",
        "Type9", "Type10", "Type11", "Type12",
        "Type13", "Type14", "Type15", "Type16",
        "Type17", "Type18", "Type19", "Type20"
    };

    [JsonIgnore]
    public ObservableCollection<string> TargetCurveTypeList { get; } = new()
    {
        " ", "High Frequency Roll Off 1", "High Frequency Roll Off 2"
    };

    [JsonIgnore]
    public ObservableCollection<string> MultEQTypeList { get; } = new()
    {
        "MultEQ", "MultEQXT", "MultEQXT32"
    };

    [JsonIgnore]
    public ObservableCollection<string> CrossoverList { get; } = new()
    {
        " ", "40", "60", "80", "90", "100", "110", "120", "150", "180", "200", "250", "F"
    };

    [JsonIgnore]
    public ObservableCollection<string> SpeakerTypeList { get; } = new() { " ", "S", "L" };

    [JsonIgnore]
    public ObservableCollection<string> ChannelSetupList { get; } = new() { "N", "S", "E" };

    [JsonIgnore]
    public ObservableCollection<string> AudyDynSetList { get; } = new() { "H", "M", "L" };

    [JsonIgnore]
    public ObservableCollection<string> AudyEqSetList { get; } = new() { "Audy", "Flat" };

    [JsonIgnore]
    public ObservableCollection<int> AudyEqRefList { get; } = new() { 0, 5, 10, 15 };

    [JsonIgnore]
    public ObservableCollection<int> AudyLfcLevList { get; } = new() { 1, 2, 3, 4, 5, 6, 7 };

    [JsonIgnore]
    public ObservableCollection<decimal> SelectedChLevelList { get; } = new()
    {
        // Strictly increasing 0.5 dB steps from -12 to +12. The original
        // hand-typed literal accidentally skipped 8.0 dB and listed 9.0 dB
        // twice (a transposition between 7.5 and 9.5).
        -12m, -11.5m, -11m, -10.5m, -10m, -9.5m, -9m, -8.5m, -8m, -7.5m, -7m, -6.5m,
        -6m, -5.5m, -5m, -4.5m, -4m, -3.5m, -3m, -2.5m, -2m, -1.5m, -1m, -0.5m,
        0m, 0.5m, 1m, 1.5m, 2m, 2.5m, 3m, 3.5m, 4m, 4.5m, 5m, 5.5m,
        6m, 6.5m, 7m, 7.5m, 8m, 8.5m, 9m, 9.5m, 10m, 10.5m, 11m, 11.5m, 12m
    };

    [JsonIgnore]
    public ObservableCollection<string> AudyFinFlgList { get; } = new() { "Fin", "NotFin" };
}
