using System.Globalization;
using System.Windows.Data;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.Converters;

/// <summary>Etiqueta legible para los enums de operación administrativa.</summary>
public sealed class EnumTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            TipoCarrera tipo => EnumTextos.TipoCarrera(tipo),
            Sexo sexo => EnumTextos.Sexo(sexo),
            RegimenCursada regimen => EnumTextos.RegimenCursada(regimen),
            TipoPeriodo tipo => EnumTextos.TipoPeriodo(tipo),
            Turno turno => EnumTextos.Turno(turno),
            ModalidadDictado modalidad => EnumTextos.ModalidadDictado(modalidad),
            CargoDocente cargo => EnumTextos.CargoDocente(cargo),
            TipoRequisito requisito => EnumTextos.TipoRequisito(requisito),
            null => string.Empty,
            _ => value.ToString() ?? string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
