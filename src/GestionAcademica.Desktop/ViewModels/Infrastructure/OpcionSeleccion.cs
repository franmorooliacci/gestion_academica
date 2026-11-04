namespace GestionAcademica.Desktop.ViewModels.Infrastructure;

/// <summary>
/// Elemento de un ComboBox: valor de dominio + texto visible.
/// Evita que las vistas tengan que conocer los DTOs para mostrarlos.
/// </summary>
public sealed record OpcionSeleccion<T>(T Valor, string Texto)
{
    public override string ToString() => Texto;
}
