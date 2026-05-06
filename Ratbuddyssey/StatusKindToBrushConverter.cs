using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ratbuddyssey;

/// <summary>
/// Converts a <see cref="ChannelRowViewModel.StatusKind"/> string ("Ok"/"Warning"/"Error")
/// into an <see cref="IBrush"/> for the validation badge in the channels grid.
/// </summary>
public sealed class StatusKindToBrushConverter : IValueConverter, IMultiValueConverter
{
    private static readonly IBrush OkBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));      // green 800
    private static readonly IBrush WarnBrush = new SolidColorBrush(Color.FromRgb(0xED, 0x6C, 0x02));    // amber 800
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));   // red 700

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string) switch
        {
            "Error" => ErrorBrush,
            "Warning" => WarnBrush,
            _ => OkBrush,
        };

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        => values != null && values.Count > 0 ? Convert(values[0], targetType, parameter, culture) : OkBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
