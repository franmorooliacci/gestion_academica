namespace GestionAcademica.Desktop.Services;

/// <summary>
/// Confirmación de acciones destructivas. Es el ÚNICO uso permitido de MessageBox en el escritorio.
/// </summary>
public interface IConfirmationService
{
    bool Confirm(string title, string message);
}
