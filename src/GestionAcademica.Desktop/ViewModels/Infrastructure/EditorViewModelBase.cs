using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionAcademica.Desktop.ViewModels.Infrastructure;

/// <summary>
/// Base de los formularios: título, alta/edición y validación con DataAnnotations.
/// No conoce la API ni la vista; solo produce los requests del contrato.
/// </summary>
public abstract partial class EditorViewModelBase : ObservableValidator
{
    private bool _mostroErrores;

    public bool EsAlta { get; protected init; }

    public string Titulo { get; protected init; } = string.Empty;

    /// <summary>Valida todo el formulario y devuelve el primer mensaje de error, o null si es válido.</summary>
    public virtual string? Validate()
    {
        _mostroErrores = true;
        ValidateAllProperties();

        var firstError = GetErrors().FirstOrDefault()?.ErrorMessage;
        return firstError ?? ValidateAdicional();
    }

    /// <summary>
    /// Revalidación en vivo de un campo, solo después del primer intento de guardado,
    /// para no pintar en rojo campos recién abiertos.
    /// </summary>
    protected void Revalidate(object? value, string propertyName)
    {
        if (_mostroErrores)
            ValidateProperty(value, propertyName);
    }

    /// <summary>Hook para reglas que no se expresan con atributos (p. ej. contraseña).</summary>
    protected virtual string? ValidateAdicional() => null;
}
