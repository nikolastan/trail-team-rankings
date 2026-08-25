using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TrailTeamRankings.App;

/// <summary>Visible when the bound string has content; Collapsed when null/blank.</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
