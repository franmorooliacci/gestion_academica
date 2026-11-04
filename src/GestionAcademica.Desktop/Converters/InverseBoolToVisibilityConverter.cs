using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestionAcademica.Desktop.Converters;

/// <summary>Visible cuando el valor booleano es <c>false</c>; colapsado cuando es <c>true</c>.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}
