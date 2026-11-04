using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

public partial class LoginViewModel(IAuthApi auth, SessionService session) : ObservableObject
{
    public event EventHandler? LoginSucceeded;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Ingresá usuario y contraseña.");
            return;
        }

        IsBusy = true;
        SetError(null);

        try
        {
            var response = await auth.LoginAsync(
                new LoginRequest(Username.Trim(), Password, ClientApp.Desktop),
                CancellationToken.None);

            session.Set(response);
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException ex)
        {
            SetError(ex.Message);
        }
        catch (HttpRequestException)
        {
            SetError("No se pudo conectar con la API. Verificá que esté corriendo.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetError(string? message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrWhiteSpace(message);
    }
}
