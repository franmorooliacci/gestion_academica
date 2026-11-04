using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>
/// Pantalla de referencia del escritorio: búsqueda con debounce, grilla densa,
/// formulario lateral y errores de API centralizados.
/// </summary>
public partial class PersonasViewModel : PageViewModelBase
{
    private const int SearchDebounceMilliseconds = 300;

    private readonly IPersonasApi _api;
    private readonly IRolesApi _rolesApi;
    private readonly IConfirmationService _confirmation;
    private readonly List<PersonaDto> _todas = [];

    private CancellationTokenSource? _searchCts;

    public PersonasViewModel(IPersonasApi api, IRolesApi rolesApi, IConfirmationService confirmation)
    {
        _api = api;
        _rolesApi = rolesApi;
        _confirmation = confirmation;
    }

    public ObservableCollection<PersonaDto> Personas { get; } = [];

    public ObservableCollection<RolOpcion> RolesDisponibles { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _soloActivos = true;

    [ObservableProperty]
    private PersonaDto? _selectedPersona;

    [ObservableProperty]
    private PersonaEditorViewModel? _editor;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    [ObservableProperty]
    private bool _mostrarVacio;

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        await LoadRolesAsync(ct);
        await LoadAsync(ct);
    }

    public override void CancelPendingWork()
    {
        base.CancelPendingWork();
        _searchCts?.Cancel();
    }

    partial void OnSearchTextChanged(string value) => _ = SearchDebouncedAsync();

    partial void OnSoloActivosChanged(bool value) => Rebuild();

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    [RelayCommand]
    private void Nueva()
    {
        Editor = new PersonaEditorViewModel(null, RolesDisponibles.Select(rol => rol.Rol).ToArray());
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void Editar(PersonaDto? persona)
    {
        persona ??= SelectedPersona;
        if (persona is null)
            return;

        Editor = new PersonaEditorViewModel(persona, RolesDisponibles.Select(rol => rol.Rol).ToArray());
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void CancelarEdicion()
    {
        IsEditorOpen = false;
        Editor = null;
        SetError(null);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Editor is null)
            return;

        var validationError = Editor.Validate();
        if (validationError is not null)
        {
            SetError(validationError);
            return;
        }

        IsBusy = true;
        SetError(null);

        try
        {
            if (Editor.EsAlta)
                await _api.CreateAsync(Editor.ToCreateRequest(), Token);
            else
                await _api.UpdateAsync(Editor.Id!.Value, Editor.ToUpdateRequest(), Token);

            IsEditorOpen = false;
            Editor = null;
            await LoadAsync(Token);
        }
        catch (OperationCanceledException)
        {
            // La pantalla se cerró mientras se guardaba: no hay nada que mostrar.
        }
        catch (ApiException ex)
        {
            SetError(ex.Message);
        }
        catch (HttpRequestException)
        {
            SetError("No se pudo conectar con la API.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        var persona = SelectedPersona;
        if (persona is null)
            return;

        var confirmado = _confirmation.Confirm(
            "Eliminar persona",
            $"¿Eliminar a {persona.Apellido}, {persona.Nombre} (DNI {persona.Dni})?\n\nLa persona quedará inactiva.");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteAsync(persona.Id, Token);
            await LoadAsync(Token);
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
            SetError("No se pudo conectar con la API.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchDebouncedAsync()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();

        var cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
        _searchCts = cts;

        try
        {
            await Task.Delay(SearchDebounceMilliseconds, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await LoadAsync(cts.Token);
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        IsBusy = true;
        SetError(null);

        try
        {
            var personas = await _api.GetAllAsync(SearchText.Trim(), ct);

            _todas.Clear();
            _todas.AddRange(personas);
        }
        catch (OperationCanceledException)
        {
            return;
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
            Rebuild();
        }
    }

    private async Task LoadRolesAsync(CancellationToken ct)
    {
        try
        {
            var roles = await _rolesApi.GetAllAsync(ct);

            RolesDisponibles.Clear();
            foreach (var rol in roles.Where(rol => rol.Nombre != AppRoles.Administrativo))
                RolesDisponibles.Add(new RolOpcion(rol, false));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            SetError("No se pudieron cargar los roles. Revisá la conexión con la API.");
        }
    }

    private void Rebuild()
    {
        var selectedId = SelectedPersona?.Id;
        var visibles = SoloActivos ? _todas.Where(persona => persona.Activo) : _todas;

        Personas.Clear();
        foreach (var persona in visibles)
            Personas.Add(persona);

        SelectedPersona = Personas.FirstOrDefault(persona => persona.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Personas.Count;
        MostrarVacio = !IsBusy && Personas.Count == 0;
    }
}
