using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Audyssey;
using Audyssey.MultEQApp;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ratbuddyssey;

/// <summary>
/// Display-only wrapper around a <see cref="DetectedChannel"/>. Adds the
/// AudysseyOne-derived hardware-limits validation and a friendly speaker name
/// without polluting the JSON-serialized model class.
/// </summary>
public partial class ChannelRowViewModel : ObservableObject, IDisposable
{
    private readonly Func<decimal> _subTrimFloorProvider;
    private bool _disposed;

    public DetectedChannel Channel { get; }

    [ObservableProperty]
    private string _speakerName = string.Empty;

    [ObservableProperty]
    private ChannelValidation _validation = ChannelValidation.Ok;

    /// <summary>UI-friendly badge color name: "Ok", "Warning", or "Error".</summary>
    [ObservableProperty]
    private string _statusKind = "Ok";

    public ChannelRowViewModel(DetectedChannel channel, Func<decimal> subwooferTrimFloorProvider)
    {
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _subTrimFloorProvider = subwooferTrimFloorProvider ?? (() => -AudysseyHardwareQuirks.MaxAbsoluteTrimDb);
        SpeakerName = AudysseyChannelNames.Friendly(channel.CommandId);
        channel.PropertyChanged += OnChannelChanged;
        Refresh();
    }

    private void OnChannelChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DetectedChannel.CommandId))
        {
            SpeakerName = AudysseyChannelNames.Friendly(Channel.CommandId);
        }
        // Any field can affect validation; cheap to recompute.
        Refresh();
    }

    public void Refresh()
    {
        Validation = ChannelLimitsValidator.Validate(Channel, _subTrimFloorProvider());
        StatusKind = Validation.Severity switch
        {
            ValidationSeverity.Error => "Error",
            ValidationSeverity.Warning => "Warning",
            _ => "Ok",
        };
    }

    public bool MatchesFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        var cmp = StringComparison.OrdinalIgnoreCase;
        return (Channel.CommandId?.Contains(filter, cmp) ?? false)
            || (SpeakerName?.Contains(filter, cmp) ?? false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Channel.PropertyChanged -= OnChannelChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
