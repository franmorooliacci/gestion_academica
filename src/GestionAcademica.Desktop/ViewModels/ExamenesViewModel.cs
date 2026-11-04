using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Fila de la grilla de mesas de examen con el período ya formateado.</summary>
public sealed record ExamenRow(ExamenDto Examen)
{
    public int Id => Examen.Id;

    public string MateriaNombre => Examen.MateriaNombre;

    public string PeriodoTexto => Examen.PeriodoAnio is not null && Examen.PeriodoTipo is not null
        ? EnumTextos.Periodo(Examen.PeriodoAnio.Value, Examen.PeriodoTipo.Value)
        : "Sin período";

    public DateTime Fecha => Examen.Fecha;

    public string? Aula => Examen.Aula;

    public bool InscripcionFinalesAbierta => Examen.InscripcionFinalesAbierta;
}

/// <summary>Operación de mesas de examen: programación por materia y período.</summary>
public partial class ExamenesViewModel : PageViewModelBase
{
    private readonly IExamenesApi _api;
    private readonly IMateriasApi _materiasApi;
    private readonly IPeriodosApi _periodosApi;
    private readonly IConfirmationService _confirmation;
    private readonly List<ExamenRow> _todas = [];

    private List<MateriaDto> _materias = [];
    private List<PeriodoDto> _periodos = [];
    private Dictionary<int, MateriaDto> _materiasPorId = [];

    public ExamenesViewModel(
        IExamenesApi api,
        IMateriasApi materiasApi,
        IPeriodosApi periodosApi,
        IConfirmationService confirmation,
        SessionService session)
        : base(session)
    {
        _api = api;
        _materiasApi = materiasApi;
        _periodosApi = periodosApi;
        _confirmation = confirmation;
    }

    public ObservableCollection<ExamenRow> Examenes { get; } = [];

    public ObservableCollection<OpcionSeleccion<MateriaDto?>> MateriasFiltro { get; } = [];

    public ObservableCollection<OpcionSeleccion<PeriodoDto?>> PeriodosFiltro { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private OpcionSeleccion<MateriaDto?>? _materiaFiltro;

    [ObservableProperty]
    private OpcionSeleccion<PeriodoDto?>? _periodoFiltro;

    [ObservableProperty]
    private ExamenRow? _selectedExamen;

    [ObservableProperty]
    private ExamenEditorViewModel? _editor;

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

    partial void OnPeriodoFiltroChanged(OpcionSeleccion<PeriodoDto?>? value) => Rebuild();

    protected override void OnCarreraActivaChanged() => Rebuild();

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    [RelayCommand]
    private void Nueva()
    {
        Editor = new ExamenEditorViewModel(null, _materias, _periodos, CarreraActivaId);
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void Editar(ExamenRow? examen)
    {
        examen ??= SelectedExamen;
        if (examen is null)
            return;

        Editor = new ExamenEditorViewModel(examen.Examen, _materias, _periodos);
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
        var examen = SelectedExamen;
        if (examen is null)
            return;

        var confirmado = _confirmation.Confirm(
            "Eliminar mesa de examen",
            $"¿Eliminar la mesa de {examen.MateriaNombre} del {examen.Fecha:dd/MM/yyyy HH:mm}?");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteAsync(examen.Id, Token);
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
            var examenes = await _api.GetAllAsync(null, null, ct);

            _todas.Clear();
            _todas.AddRange(examenes.Select(examen => new ExamenRow(examen)));
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
            var periodos = await _periodosApi.GetAllAsync(ct);

            _materias = materias.ToList();
            _periodos = periodos.ToList();
            _materiasPorId = _materias.ToDictionary(materia => materia.Id);

            MateriasFiltro.Clear();
            MateriasFiltro.Add(new OpcionSeleccion<MateriaDto?>(null, "Todas las materias"));
            foreach (var materia in _materias)
                MateriasFiltro.Add(new OpcionSeleccion<MateriaDto?>(materia, $"{materia.Codigo} · {materia.Nombre}"));

            PeriodosFiltro.Clear();
            PeriodosFiltro.Add(new OpcionSeleccion<PeriodoDto?>(null, "Todos los períodos"));
            foreach (var periodo in _periodos)
            {
                PeriodosFiltro.Add(new OpcionSeleccion<PeriodoDto?>(
                    periodo,
                    EnumTextos.Periodo(periodo.Anio, periodo.Tipo)));
            }

            MateriaFiltro = MateriasFiltro[0];
            PeriodoFiltro = PeriodosFiltro[0];
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
        var selectedId = SelectedExamen?.Id;
        var term = SearchText.Trim();
        var idMateria = MateriaFiltro?.Valor?.Id;
        var idPeriodo = PeriodoFiltro?.Valor?.Id;

        var visibles = _todas.Where(row =>
            (idMateria is null || row.Examen.IdMateria == idMateria) &&
            (idPeriodo is null || row.Examen.IdPeriodo == idPeriodo) &&
            PerteneceACarreraActiva(row.Examen.IdMateria) &&
            (term.Length == 0 ||
             row.MateriaNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             (row.Aula?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)));

        Examenes.Clear();
        foreach (var examen in visibles)
            Examenes.Add(examen);

        SelectedExamen = Examenes.FirstOrDefault(examen => examen.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Examenes.Count;
        MostrarVacio = !IsBusy && Examenes.Count == 0;
    }
}
