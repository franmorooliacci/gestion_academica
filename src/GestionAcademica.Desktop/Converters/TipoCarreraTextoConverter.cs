using System.Globalization;
using System.Windows.Data;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.Converters;

/// <summary>Etiqueta legible para el enum <see cref="TipoCarrera"/>.</summary>
public sealed class TipoCarreraTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TipoCarrera tipo
            ? tipo switch
            {
                TipoCarrera.Curso => "Curso",
                TipoCarrera.Pregrado => "Pregrado",
                TipoCarrera.Grado => "Grado",
                TipoCarrera.Postgrado => "Postgrado",
                _ => tipo.ToString()
            }
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
