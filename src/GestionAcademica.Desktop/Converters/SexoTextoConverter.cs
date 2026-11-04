using System.Globalization;
using System.Windows.Data;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.Converters;

/// <summary>Etiqueta legible para el enum <see cref="Sexo"/> en el formulario.</summary>
public sealed class SexoTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Sexo sexo
            ? sexo switch
            {
                Sexo.Femenino => "Femenino",
                Sexo.Masculino => "Masculino",
                Sexo.NoBinario => "No binario",
                Sexo.Otro => "Otro",
                Sexo.PrefieroNoDecir => "Prefiero no decir",
                _ => sexo.ToString()
            }
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
