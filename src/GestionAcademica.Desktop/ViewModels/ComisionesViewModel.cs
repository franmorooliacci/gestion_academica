using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Fila de la grilla de comisiones: DTO + período resuelto.</summary>
public sealed record ComisionRow(ComisionDto Comision, string PeriodoTexto)
{
    public int Id => Comision.Id;

    public string MateriaNombre => Comision.MateriaNombre;

    public string Codigo => Comision.Codigo;

    public Turno Turno => Comision.Turno;

    public ModalidadDictado ModalidadDictado => Comision.ModalidadDictado;

    public int? CupoMaximo => Comision.CupoMaximo;

    public int? ListaEsperaMaxima => Comision.ListaEsperaMaxima;

    public bool Activo => Comision.Activo;
}

/// <summary>Operación de comisiones: cupos, listas de espera y turnos.</summary>
public partial class ComisionesViewModel : PageViewModelBase
{
    private readonly IComisionesApi _api;
    private readonly IMateriasApi _materiasApi;
    private readonly IPeriodosApi _periodosApi;
    private readonly IConfirmationService _confirmation;
    private readonly List<ComisionRow> _todas = [];

    private List<MateriaDto> _materias = [];
    private List<PeriodoDto> _periodos = [];
    private Dictionary<int, MateriaDto> _materiasPorId = [];

    public ComisionesViewModel(
        IComisionesApi api,
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

    public ObservableCollection<ComisionRow> Comisiones { get; } = [];

    public ObservableCollection<OpcionSeleccion<MateriaDto?>> MateriasFiltro { get; } = [];

    public ObservableCollection<OpcionSeleccion<PeriodoDto?>> PeriodosFiltro { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private OpcionSeleccion<MateriaDto?>? _materiaFiltro;

    [ObservableProperty]
    private OpcionSeleccion<PeriodoDto?>? _periodoFiltro;

    [ObservableProperty]
    private bool _soloActivas = true;

    [ObservableProperty]
    private ComisionRow? _selectedComision;

    [ObservableProperty]
    private ComisionEditorViewModel? _editor;

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

    partial void OnSoloActivasChanged(bool value) => Rebuild();

    protected override void OnCarreraActivaChanged() => Rebuild();

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    [RelayCommand]
    private void Nueva()
    {
        Editor = new ComisionEditorViewModel(null, _materias, _periodos, CarreraActivaId);
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private void Editar(ComisionRow? comision)
    {
        comision ??= SelectedComision;
        if (comision is null)
            return;

        Editor = new ComisionEditorViewModel(comision.Comision, _materias, _periodos);
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
        var comision = SelectedComision;
        if (comision is null)
            return;

        var confirmado = _confirmation.Confirm(
            "Eliminar comisión",
            $"¿Eliminar la comisión {comision.Codigo} de {comision.MateriaNombre}?\n\nQuedará inactiva.");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteAsync(comision.Id, Token);
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
            var comisiones = await _api.GetAllAsync(null, null, ct);

            var periodos = _periodos.ToDictionary(
                periodo => periodo.Id,
                periodo => EnumTextos.Periodo(periodo.Anio, periodo.Tipo));

            _todas.Clear();
            foreach (var comision in comisiones)
            {
                var periodoTexto = periodos.GetValueOrDefault(comision.IdPeriodo, string.Empty);
                _todas.Add(new ComisionRow(comision, periodoTexto));
            }
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
        var selectedId = SelectedComision?.Id;
        var term = SearchText.Trim();
        var idMateria = MateriaFiltro?.Valor?.Id;
        var idPeriodo = PeriodoFiltro?.Valor?.Id;

        var visibles = _todas.Where(row =>
            (!SoloActivas || row.Activo) &&
            (idMateria is null || row.Comision.IdMateria == idMateria) &&
            (idPeriodo is null || row.Comision.IdPeriodo == idPeriodo) &&
            PerteneceACarreraActiva(row.Comision.IdMateria) &&
            (term.Length == 0 ||
             row.Codigo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             row.MateriaNombre.Contains(term, StringComparison.OrdinalIgnoreCase)));

        Comisiones.Clear();
        foreach (var comision in visibles)
            Comisiones.Add(comision);

        SelectedComision = Comisiones.FirstOrDefault(comision => comision.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Comisiones.Count;
        MostrarVacio = !IsBusy && Comisiones.Count == 0;
    }
}
