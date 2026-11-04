using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.Services;
using Wpf.Ui.Abstractions.Controls;

namespace GestionAcademica.Desktop.ViewModels.Infrastructure;

/// <summary>
/// Base de las pantallas de operación: estado de carga/error/editor y ciclo de vida
/// de navegación con cancelación. Las pantallas concretas solo implementan la carga.
/// </summary>
public abstract partial class PageViewModelBase : ObservableObject, INavigationAware
{
    private readonly SessionService? _session;
    private CancellationTokenSource? _lifetimeCts;
    private bool _initialized;
    private bool _observingSession;

    protected PageViewModelBase()
    {
    }

    /// <summary>Pantalla que participa del contexto de sesión (carrera activa).</summary>
    protected PageViewModelBase(SessionService session) => _session = session;

    /// <summary>Carrera elegida en el shell; <c>null</c> significa "todas las carreras".</summary>
    protected int? CarreraActivaId => _session?.CarreraActiva?.Id;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isEditorOpen;

    /// <summary>Token de vida de la pantalla: se cancela al navegar afuera.</summary>
    protected CancellationToken Token => _lifetimeCts?.Token ?? CancellationToken.None;

    public virtual Task OnNavigatedToAsync()
    {
        if (_session is not null && !_observingSession)
        {
            _session.CarreraActivaChanged += OnSessionCarreraChanged;
            _observingSession = true;
        }

        return InitializeAsync();
    }

    public virtual Task OnNavigatedFromAsync()
    {
        CancelPendingWork();
        return Task.CompletedTask;
    }

    private void OnSessionCarreraChanged(object? sender, EventArgs e) => OnCarreraActivaChanged();

    /// <summary>Reacción al cambio de carrera activa en el shell.</summary>
    protected virtual void OnCarreraActivaChanged()
    {
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        _lifetimeCts?.Dispose();
        _lifetimeCts = new CancellationTokenSource();

        await InitializeCoreAsync(_lifetimeCts.Token);
    }

    /// <summary>
    /// Cancela el trabajo en curso al salir de la pantalla y deja el ViewModel listo
    /// para volver a inicializarse si la página se reutiliza.
    /// </summary>
    public virtual void CancelPendingWork()
    {
        // También se ejecuta desde Unloaded: ahí se suelta la suscripción al contexto
        // de sesión aunque la navegación no haya disparado OnNavigatedFromAsync.
        if (_session is not null && _observingSession)
        {
            _session.CarreraActivaChanged -= OnSessionCarreraChanged;
            _observingSession = false;
        }

        _lifetimeCts?.Cancel();
        _initialized = false;
    }

    /// <summary>Inicialización de la pantalla. Por defecto delega en <see cref="LoadAsync"/>.</summary>
    protected virtual Task InitializeCoreAsync(CancellationToken ct) => LoadAsync(ct);

    protected abstract Task LoadAsync(CancellationToken ct);

    /// <summary>
    /// Ejecuta una operación con el estado de carga y la traducción uniforme de errores de API.
    /// </summary>
    protected async Task RunGuardedAsync(
        Func<CancellationToken, Task> operation,
        string connectionMessage = "No se pudo conectar con la API.")
    {
        IsBusy = true;
        SetError(null);

        try
        {
            await operation(Token);
        }
        catch (OperationCanceledException)
        {
            // La pantalla se cerró mientras la operación estaba en curso.
        }
        catch (ApiException ex)
        {
            SetError(ex.Message);
        }
        catch (HttpRequestException)
        {
            SetError(connectionMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void SetError(string? message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrWhiteSpace(message);
    }
}
