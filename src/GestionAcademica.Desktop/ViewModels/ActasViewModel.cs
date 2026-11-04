using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Fila del acta de una mesa: alumno, estado y nota registrados por el profesor.</summary>
public sealed record ActaRow(InscripcionExamenDto Inscripcion)
{
    public string PersonaNombre => Inscripcion.PersonaNombre;

    public EstadoInscripcionExamen Estado => Inscripcion.Estado;

    public string EstadoTexto => EnumTextos.EstadoInscripcionExamen(Estado);

    public bool EsPresente => Estado == EstadoInscripcionExamen.Presente;

    public bool EsAusente => Estado == EstadoInscripcionExamen.Ausente;

    public string NotaTexto => Inscripcion.Nota is { } nota ? nota.ToString("0.##") : "—";

    public DateTime FechaInscripcion => Inscripcion.FechaInscripcion;
}

/// <summary>
/// Consulta de solo lectura de las actas de examen. El cierre y la carga de notas
/// son responsabilidad del profesor desde la web; acá solo se consultan.
/// </summary>
public partial class ActasViewModel : PageViewModelBase
{
    private const int SearchDebounceMilliseconds = 300;

    private readonly IExamenesApi _api;
    private readonly IMateriasApi _materiasApi;
    private readonly IPeriodosApi _periodosApi;
    private readonly List<ExamenRow> _todas = [];

    private Dictionary<int, MateriaDto> _materiasPorId = [];
    private CancellationTokenSource? _actaCts;
    private CancellationTokenSource? _searchCts;

    public ActasViewModel(
        IExamenesApi api,
        IMateriasApi materiasApi,
        IPeriodosApi periodosApi,
        SessionService session)
        : base(session)
    {
        _api = api;
        _materiasApi = materiasApi;
        _periodosApi = periodosApi;
    }

    public ObservableCollection<ExamenRow> Mesas { get; } = [];

    public ObservableCollection<ActaRow> Acta { get; } = [];

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
    private bool _isLoadingActa;

    [ObservableProperty]
    private string _actaTitulo = "Acta";

    [ObservableProperty]
    private string _actaDetalle = "Seleccioná una mesa para consultar su acta.";

    [ObservableProperty]
    private string _actaResumen = string.Empty;

    [ObservableProperty]
    private bool _mostrarSeleccionVacia = true;

    [ObservableProperty]
    private bool _mostrarActaVacia;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    [ObservableProperty]
    private bool _mostrarVacio;

    protected override async Task InitializeCoreAsync(CancellationToken ct)
    {
        // Al volver a la pantalla no se reutiliza el acta anterior: se recarga completa.
        SelectedExamen = null;

        await LoadCatalogosAsync(ct);
        await LoadAsync(ct);
    }

    public override void CancelPendingWork()
    {
        base.CancelPendingWork();
        _searchCts?.Cancel();
        _actaCts?.Cancel();
        _actaCts = null;
    }

    partial void OnSearchTextChanged(string value) => _ = SearchDebouncedAsync();

    partial void OnMateriaFiltroChanged(OpcionSeleccion<MateriaDto?>? value) => Rebuild();

    partial void OnPeriodoFiltroChanged(OpcionSeleccion<PeriodoDto?>? value) => Rebuild();

    partial void OnSelectedExamenChanged(ExamenRow? value) => _ = LoadActaAsync(value);

    protected override void OnCarreraActivaChanged() => Rebuild();

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

        Rebuild();
    }

    [RelayCommand]
    private async Task RefrescarAsync()
    {
        await LoadAsync(Token);

        if (SelectedExamen is not null)
            await LoadActaAsync(SelectedExamen);
    }

    [RelayCommand]
    private Task VerActaAsync() => LoadActaAsync(SelectedExamen);

    [RelayCommand]
    private void CerrarActa() => SelectedExamen = null;

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

            _materiasPorId = materias.ToDictionary(materia => materia.Id);

            MateriasFiltro.Clear();
            MateriasFiltro.Add(new OpcionSeleccion<MateriaDto?>(null, "Todas las materias"));
            foreach (var materia in materias.OrderBy(materia => materia.Nombre))
                MateriasFiltro.Add(new OpcionSeleccion<MateriaDto?>(materia, $"{materia.Codigo} · {materia.Nombre}"));

            PeriodosFiltro.Clear();
            PeriodosFiltro.Add(new OpcionSeleccion<PeriodoDto?>(null, "Todos los períodos"));
            foreach (var periodo in periodos.OrderByDescending(periodo => periodo.Anio).ThenBy(periodo => periodo.Tipo))
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

    private async Task LoadActaAsync(ExamenRow? mesa)
    {
        // La selección cambió: la carga anterior deja de interesar.
        _actaCts?.Cancel();
        _actaCts = null;

        if (mesa is null)
        {
            Acta.Clear();
            ActaResumen = string.Empty;
            ActaTitulo = "Acta";
            ActaDetalle = "Seleccioná una mesa para consultar su acta.";
            MostrarSeleccionVacia = true;
            MostrarActaVacia = false;
            IsLoadingActa = false;
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
        _actaCts = cts;

        Acta.Clear();
        ActaResumen = string.Empty;
        ActaTitulo = $"Acta · {mesa.MateriaNombre}";
        ActaDetalle = BuildActaDetalle(mesa);
        MostrarSeleccionVacia = false;
        MostrarActaVacia = false;
        IsLoadingActa = true;
        SetError(null);

        try
        {
            var inscripciones = await _api.GetInscripcionesAsync(mesa.Id, cts.Token);

            if (cts.IsCancellationRequested)
                return;

            foreach (var inscripcion in inscripciones)
                Acta.Add(new ActaRow(inscripcion));

            ActaResumen = BuildActaResumen(inscripciones);
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
            if (ReferenceEquals(_actaCts, cts))
            {
                IsLoadingActa = false;
                MostrarActaVacia = Acta.Count == 0;
            }
        }
    }

    private static string BuildActaDetalle(ExamenRow mesa)
    {
        var detalle = $"{mesa.Fecha:dd/MM/yyyy HH:mm} · {mesa.PeriodoTexto}";

        return string.IsNullOrWhiteSpace(mesa.Aula)
            ? detalle
            : $"{detalle} · {mesa.Aula}";
    }

    private static string BuildActaResumen(IReadOnlyCollection<InscripcionExamenDto> inscripciones)
    {
        var presentes = inscripciones.Count(i => i.Estado == EstadoInscripcionExamen.Presente);
        var ausentes = inscripciones.Count(i => i.Estado == EstadoInscripcionExamen.Ausente);
        var pendientes = inscripciones.Count(i => i.Estado == EstadoInscripcionExamen.Inscripto);

        var resumen = $"{Cantidad(inscripciones.Count, "inscripto", "inscriptos")} · " +
                      $"{Cantidad(presentes, "presente", "presentes")} · " +
                      $"{Cantidad(ausentes, "ausente", "ausentes")}";

        return pendientes > 0
            ? $"{resumen} · {pendientes} sin cerrar"
            : resumen;
    }

    private static string Cantidad(int cantidad, string singular, string plural)
        => $"{cantidad} {(cantidad == 1 ? singular : plural)}";

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

        Mesas.Clear();
        foreach (var mesa in visibles)
            Mesas.Add(mesa);

        // Sin selección automática: el acta se abre recién cuando el usuario elige una mesa.
        SelectedExamen = Mesas.FirstOrDefault(mesa => mesa.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Mesas.Count;
        MostrarVacio = !IsBusy && Mesas.Count == 0;
    }
}
