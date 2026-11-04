using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Operación de asignaciones docentes por materia y comisión.</summary>
public partial class AsignacionesDocentesViewModel : PageViewModelBase
{
    private readonly IAsignacionesDocentesApi _api;
    private readonly IMateriasApi _materiasApi;
    private readonly IPersonasApi _personasApi;
    private readonly IComisionesApi _comisionesApi;
    private readonly IConfirmationService _confirmation;
    private readonly List<AsignacionDocenteDto> _todas = [];

    private List<MateriaDto> _materias = [];
    private List<PersonaDto> _docentes = [];
    private List<ComisionDto> _comisiones = [];
    private Dictionary<int, MateriaDto> _materiasPorId = [];

    public AsignacionesDocentesViewModel(
        IAsignacionesDocentesApi api,
        IMateriasApi materiasApi,
        IPersonasApi personasApi,
        IComisionesApi comisionesApi,
        IConfirmationService confirmation,
        SessionService session)
        : base(session)
    {
        _api = api;
        _materiasApi = materiasApi;
        _personasApi = personasApi;
        _comisionesApi = comisionesApi;
        _confirmation = confirmation;
    }

    public ObservableCollection<AsignacionDocenteDto> Asignaciones { get; } = [];

    public ObservableCollection<OpcionSeleccion<MateriaDto?>> MateriasFiltro { get; } = [];

    public ObservableCollection<OpcionSeleccion<PersonaDto?>> DocentesFiltro { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private OpcionSeleccion<MateriaDto?>? _materiaFiltro;

    [ObservableProperty]
    private OpcionSeleccion<PersonaDto?>? _docenteFiltro;

    [ObservableProperty]
    private bool _soloActivas = true;

    [ObservableProperty]
    private AsignacionDocenteDto? _selectedAsignacion;

    [ObservableProperty]
    private AsignacionEditorViewModel? _editor;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    [ObservableProperty]
    private bool _mostrarVacio;

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        await LoadCatalogosAsync(ct);
        await LoadAsync(ct);
    }

    partial void OnSearchTextChanged(string value) => Rebuild();

    partial void OnMateriaFiltroChanged(OpcionSeleccion<MateriaDto?>? value) => Rebuild();

    partial void OnDocenteFiltroChanged(OpcionSeleccion<PersonaDto?>? value) => Rebuild();

    partial void OnSoloActivasChanged(bool value) => Rebuild();

    protected override void OnCarreraActivaChanged() => Rebuild();

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    [RelayCommand]
    private void Nueva()
    {
        Editor = new AsignacionEditorViewModel(
            null, _materias, _docentes, _comisiones, CarreraActivaId);
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void Editar(AsignacionDocenteDto? asignacion)
    {
        asignacion ??= SelectedAsignacion;
        if (asignacion is null)
            return;

        Editor = new AsignacionEditorViewModel(asignacion, _materias, _docentes, _comisiones);
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
        var editor = Editor;
        if (editor is null)
            return;

        var validationError = editor.Validate();
        if (validationError is not null)
        {
            SetError(validationError);
            return;
        }

        IsBusy = true;
        SetError(null);

        try
        {
            if (editor.EsAlta)
                await _api.CreateAsync(editor.ToCreateRequest(), Token);
            else
                await _api.UpdateAsync(editor.Id!.Value, editor.ToUpdateRequest(), Token);

            IsEditorOpen = false;
            Editor = null;
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

    [RelayCommand]
    private async Task EliminarAsync()
    {
        var asignacion = SelectedAsignacion;
        if (asignacion is null)
            return;

        var confirmado = _confirmation.Confirm(
            "Eliminar asignación",
            $"¿Eliminar la asignación de {asignacion.DocenteNombre} en {asignacion.MateriaNombre}?\n\nQuedará inactiva.");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteAsync(asignacion.Id, Token);
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

    protected override async Task LoadAsync(CancellationToken ct)
    {
        IsBusy = true;
        SetError(null);

        try
        {
            var asignaciones = await _api.GetAllAsync(null, null, ct);

            _todas.Clear();
            _todas.AddRange(asignaciones);
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

    private async Task LoadCatalogosAsync(CancellationToken ct)
    {
        try
        {
            var materias = await _materiasApi.GetAllAsync(null, ct);
            var personas = await _personasApi.GetAllAsync(null, ct);
            var comisiones = await _comisionesApi.GetAllAsync(null, null, ct);

            _materias = materias.ToList();
            _materiasPorId = _materias.ToDictionary(materia => materia.Id);
            _docentes = personas
                .Where(persona => persona.Activo &&
                                  persona.Roles.Any(rol => rol.Nombre == AppRoles.Profesor))
                .OrderBy(persona => persona.Apellido)
                .ThenBy(persona => persona.Nombre)
                .ToList();
            _comisiones = comisiones.ToList();

            MateriasFiltro.Clear();
            MateriasFiltro.Add(new OpcionSeleccion<MateriaDto?>(null, "Todas las materias"));
            foreach (var materia in _materias)
                MateriasFiltro.Add(new OpcionSeleccion<MateriaDto?>(materia, $"{materia.Codigo} · {materia.Nombre}"));

            DocentesFiltro.Clear();
            DocentesFiltro.Add(new OpcionSeleccion<PersonaDto?>(null, "Todos los docentes"));
            foreach (var docente in _docentes)
                DocentesFiltro.Add(new OpcionSeleccion<PersonaDto?>(docente, $"{docente.Apellido}, {docente.Nombre}"));

            MateriaFiltro = MateriasFiltro[0];
            DocenteFiltro = DocentesFiltro[0];
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
            SetError("No se pudieron cargar los catálogos. Revisá la conexión con la API.");
        }
    }

    private bool PerteneceACarreraActiva(int idMateria)
        => CarreraActivaId is not int idCarrera
           || (_materiasPorId.TryGetValue(idMateria, out var materia)
               && materia.IdCarrera == idCarrera);

    private void Rebuild()
    {
        var selectedId = SelectedAsignacion?.Id;
        var term = SearchText.Trim();
        var idMateria = MateriaFiltro?.Valor?.Id;
        var idPersona = DocenteFiltro?.Valor?.Id;

        var visibles = _todas.Where(asignacion =>
            (!SoloActivas || asignacion.Activo) &&
            (idMateria is null || asignacion.IdMateria == idMateria) &&
            (idPersona is null || asignacion.IdPersona == idPersona) &&
            PerteneceACarreraActiva(asignacion.IdMateria) &&
            (term.Length == 0 ||
             asignacion.DocenteNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             asignacion.MateriaNombre.Contains(term, StringComparison.OrdinalIgnoreCase)));

        Asignaciones.Clear();
        foreach (var asignacion in visibles)
            Asignaciones.Add(asignacion);

        SelectedAsignacion = Asignaciones.FirstOrDefault(asignacion => asignacion.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Asignaciones.Count;
        MostrarVacio = !IsBusy && Asignaciones.Count == 0;
    }
}
