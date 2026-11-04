using System.Globalization;
using System.Windows.Data;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Converters;

/// <summary>Muestra los roles de una persona como texto plano en la grilla.</summary>
public sealed class RolesTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable<RolDto> roles
            ? string.Join(" · ", roles.Select(rol => rol.Nombre))
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
