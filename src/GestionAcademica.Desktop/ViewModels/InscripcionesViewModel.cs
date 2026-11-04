using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Fila de la grilla de inscripciones a comisión con materia y período resueltos.</summary>
public sealed record InscripcionComisionRow(
    InscripcionComisionDto Inscripcion,
    int? IdMateria,
    int? IdPeriodo,
    string PeriodoTexto)
{
    public int Id => Inscripcion.Id;

    public string PersonaNombre => Inscripcion.PersonaNombre;

    public string MateriaNombre => Inscripcion.MateriaNombre;

    public string ComisionCodigo => Inscripcion.ComisionCodigo;

    public EstadoInscripcionComision Estado => Inscripcion.Estado;

    public string EstadoTexto => EnumTextos.EstadoInscripcionComision(Estado);

    public bool EsEnEspera => Estado == EstadoInscripcionComision.EnEspera;

    public bool EsBaja => Estado == EstadoInscripcionComision.Baja;

    public string CondicionTexto => Inscripcion.Condicion is { } condicion
        ? EnumTextos.CondicionCursada(condicion)
        : "—";

    public DateTime FechaInscripcion => Inscripcion.FechaInscripcion;
}

/// <summary>Consulta de solo lectura de las inscripciones a comisión.</summary>
public partial class InscripcionesViewModel : PageViewModelBase
{
    private readonly IInscripcionesComisionApi _api;
    private readonly IMateriasApi _materiasApi;
    private readonly IPeriodosApi _periodosApi;
    private readonly IComisionesApi _comisionesApi;
    private readonly List<InscripcionComisionRow> _todas = [];

    private Dictionary<int, ComisionDto> _comisionesPorId = [];
    private Dictionary<int, MateriaDto> _materiasPorId = [];
    private Dictionary<int, string> _periodosPorId = [];

    public InscripcionesViewModel(
        IInscripcionesComisionApi api,
        IMateriasApi materiasApi,
        IPeriodosApi periodosApi,
        IComisionesApi comisionesApi,
        SessionService session)
        : base(session)
    {
        _api = api;
        _materiasApi = materiasApi;
        _periodosApi = periodosApi;
        _comisionesApi = comisionesApi;

        EstadoFiltro = EstadosFiltro[0];
    }

    public ObservableCollection<InscripcionComisionRow> Inscripciones { get; } = [];

    public ObservableCollection<OpcionSeleccion<MateriaDto?>> MateriasFiltro { get; } = [];

    public ObservableCollection<OpcionSeleccion<PeriodoDto?>> PeriodosFiltro { get; } = [];

    public IReadOnlyList<OpcionSeleccion<EstadoInscripcionComision?>> EstadosFiltro { get; } =
    [
        new(null, "Todos los estados"),
        new(EstadoInscripcionComision.Inscripto, "Inscripto"),
        new(EstadoInscripcionComision.EnEspera, "En espera"),
        new(EstadoInscripcionComision.Baja, "Baja"),
    ];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private OpcionSeleccion<MateriaDto?>? _materiaFiltro;

    [ObservableProperty]
    private OpcionSeleccion<PeriodoDto?>? _periodoFiltro;

    [ObservableProperty]
    private OpcionSeleccion<EstadoInscripcionComision?>? _estadoFiltro;

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

    partial void OnEstadoFiltroChanged(OpcionSeleccion<EstadoInscripcionComision?>? value) => Rebuild();

    protected override void OnCarreraActivaChanged() => Rebuild();

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    protected override async Task LoadAsync(CancellationToken ct)
    {
        IsBusy = true;
        SetError(null);

        try
        {
            var inscripciones = await _api.GetAllAsync(null, ct);

            _todas.Clear();
            _todas.AddRange(inscripciones
                .Select(ToRow)
                .OrderByDescending(inscripcion => inscripcion.FechaInscripcion));
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
            var comisiones = await _comisionesApi.GetAllAsync(null, null, ct);

            _comisionesPorId = comisiones.ToDictionary(comision => comision.Id);
            _materiasPorId = materias.ToDictionary(materia => materia.Id);
            _periodosPorId = periodos.ToDictionary(
                periodo => periodo.Id,
                periodo => EnumTextos.Periodo(periodo.Anio, periodo.Tipo));

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

    private InscripcionComisionRow ToRow(InscripcionComisionDto inscripcion)
    {
        _comisionesPorId.TryGetValue(inscripcion.IdComision, out var comision);

        var periodoTexto = comision is not null
                           && _periodosPorId.TryGetValue(comision.IdPeriodo, out var periodo)
            ? periodo
            : "—";

        return new InscripcionComisionRow(
            inscripcion,
            comision?.IdMateria,
            comision?.IdPeriodo,
            periodoTexto);
    }

    private bool PerteneceACarreraActiva(int? idMateria)
    {
        if (CarreraActivaId is not int idCarrera)
            return true;

        return idMateria is int id
               && _materiasPorId.TryGetValue(id, out var materia)
               && materia.IdCarrera == idCarrera;
    }

    private void Rebuild()
    {
        var term = SearchText.Trim();
        var idMateria = MateriaFiltro?.Valor?.Id;
        var idPeriodo = PeriodoFiltro?.Valor?.Id;
        var estado = EstadoFiltro?.Valor;

        var visibles = _todas.Where(row =>
            (estado is null || row.Estado == estado) &&
            (idMateria is null || row.IdMateria == idMateria) &&
            (idPeriodo is null || row.IdPeriodo == idPeriodo) &&
            PerteneceACarreraActiva(row.IdMateria) &&
            (term.Length == 0 ||
             row.PersonaNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             row.MateriaNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             row.ComisionCodigo.Contains(term, StringComparison.OrdinalIgnoreCase)));

        Inscripciones.Clear();
        foreach (var inscripcion in visibles)
            Inscripciones.Add(inscripcion);

        TotalCount = _todas.Count;
        VisibleCount = Inscripciones.Count;
        MostrarVacio = !IsBusy && Inscripciones.Count == 0;
    }
}
