using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>
/// Contexto de sesión del shell: usuario autenticado y carrera activa del escritorio.
/// El escritorio siempre opera con el rol administrativo; el rol activo se cambia
/// desde la web del alumno/profesor.
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly SessionService _session;
    private readonly ICarrerasApi _carrerasApi;

    public ShellViewModel(SessionService session, ICarrerasApi carrerasApi)
    {
        _session = session;
        _carrerasApi = carrerasApi;

        SessionText = session.User is null
            ? "Sesión no iniciada"
            : $"{session.User.Apellido}, {session.User.Nombre} · {session.User.Username}";
    }

    public ObservableCollection<OpcionSeleccion<CarreraDto?>> Carreras { get; } = [];

    [ObservableProperty]
    private string _sessionText = string.Empty;

    [ObservableProperty]
    private OpcionSeleccion<CarreraDto?>? _carreraSeleccionada;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public async Task InitializeAsync(CancellationToken ct)
    {
        IsBusy = true;
        SetError(null);

        try
        {
            var carreras = await _carrerasApi.GetAllAsync(ct);

            Carreras.Clear();
            Carreras.Add(new OpcionSeleccion<CarreraDto?>(null, "Todas las carreras"));

            foreach (var carrera in carreras
                         .Where(carrera => carrera.Activa)
                         .OrderBy(carrera => carrera.Nombre))
            {
                Carreras.Add(new OpcionSeleccion<CarreraDto?>(carrera, carrera.Nombre));
            }

            CarreraSeleccionada = Carreras.FirstOrDefault(
                opcion => opcion.Valor?.Id == _session.CarreraActiva?.Id) ?? Carreras[0];
        }
        catch (OperationCanceledException)
        {
        }
        catch (ApiException ex)
        {
            SetError(ex.Message);
        }
        catch (HttpRequestException)
        {
            SetError("No se pudieron cargar las carreras. Revisá la conexión con la API.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnCarreraSeleccionadaChanged(OpcionSeleccion<CarreraDto?>? value)
        => _session.SetCarreraActiva(value?.Valor);

    private void SetError(string? message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrWhiteSpace(message);
    }
}
