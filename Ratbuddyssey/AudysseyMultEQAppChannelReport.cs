using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Audyssey.MultEQApp;

public partial class ChannelReport : ObservableObject
{
    [ObservableProperty]
    private int? _enSpeakerConnect;

    [ObservableProperty]
    private int? _customEnSpeakerConnect;

    [ObservableProperty]
    private bool? _isReversePolarity;

    [ObservableProperty]
    private decimal? _distance;

    public bool ShouldSerializeCustomEnSpeakerConnect() => CustomEnSpeakerConnect != null;

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var property in GetType().GetProperties())
        {
            sb.Append(property).Append('=').Append(property.GetValue(this, null)).Append("\r\n");
        }
        return sb.ToString();
    }
}
