using System.Windows;

namespace GestionAcademica.Desktop.Services;

public sealed class ConfirmationService : IConfirmationService
{
    public bool Confirm(string title, string message)
        => MessageBox.Show(
               message,
               title,
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning,
               MessageBoxResult.No) == MessageBoxResult.Yes;
}
