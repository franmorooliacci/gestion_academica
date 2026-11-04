using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionAcademica.Desktop.Services;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Fila de la grilla de materias: DTO + nombre de carrera resuelto.</summary>
public sealed record MateriaRow(MateriaDto Materia, string CarreraNombre)
{
    public int Id => Materia.Id;

    public string Codigo => Materia.Codigo;

    public string Nombre => Materia.Nombre;

    public short AnioEnCarrera => Materia.AnioEnCarrera;

    public RegimenCursada RegimenCursada => Materia.RegimenCursada;

    public int CargaHorariaTotal => Materia.CargaHorariaTotal;

    public bool Activa => Materia.Activa;
}

/// <summary>Operación de materias y correlatividades: grilla densa + editor lateral.</summary>
public partial class MateriasViewModel : PageViewModelBase
{
    private readonly IMateriasApi _api;
    private readonly ICarrerasApi _carrerasApi;
    private readonly IConfirmationService _confirmation;
    private readonly List<MateriaRow> _todas = [];

    private List<CarreraDto> _carreras = [];
    private List<MateriaDto> _materias = [];

    public MateriasViewModel(
        IMateriasApi api,
        ICarrerasApi carrerasApi,
        IConfirmationService confirmation,
        SessionService session)
        : base(session)
    {
        _api = api;
        _carrerasApi = carrerasApi;
        _confirmation = confirmation;
    }

    public ObservableCollection<MateriaRow> Materias { get; } = [];

    public ObservableCollection<OpcionSeleccion<CarreraDto?>> CarrerasFiltro { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private OpcionSeleccion<CarreraDto?>? _carreraFiltro;

    [ObservableProperty]
    private bool _soloActivas = true;

    [ObservableProperty]
    private MateriaRow? _selectedMateria;

    [ObservableProperty]
    private MateriaEditorViewModel? _editor;

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

    partial void OnCarreraFiltroChanged(OpcionSeleccion<CarreraDto?>? value) => Rebuild();

    partial void OnSoloActivasChanged(bool value) => Rebuild();

    protected override void OnCarreraActivaChanged()
    {
        SeleccionarCarreraContexto();
        Rebuild();
    }

    [RelayCommand]
    private Task RefrescarAsync() => LoadAsync(Token);

    [RelayCommand]
    private void Nueva()
    {
        Editor = new MateriaEditorViewModel(
            null,
            _carreras,
            _materias,
            CarreraFiltro?.Valor?.Id ?? CarreraActivaId);
        IsEditorOpen = true;
        SetError(null);
    }

    [RelayCommand]
    private async Task EditarAsync(MateriaRow? materia)
    {
        materia ??= SelectedMateria;
        if (materia is null)
            return;

        var editor = new MateriaEditorViewModel(materia.Materia, _carreras, _materias);
        Editor = editor;
        IsEditorOpen = true;
        SetError(null);

        await CargarCorrelatividadesAsync(editor);
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
        var materia = SelectedMateria;
        if (materia is null)
            return;

        var confirmado = _confirmation.Confirm(
            "Eliminar materia",
            $"¿Eliminar la materia {materia.Codigo} · {materia.Nombre}?\n\nQuedará inactiva.");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteAsync(materia.Id, Token);
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
    private async Task AgregarCorrelatividadAsync()
    {
        var editor = Editor;
        if (editor?.Id is not int idMateria)
            return;

        if (editor.RequisitoSeleccionado is null)
        {
            SetError("Elegí la materia requisito antes de agregar la correlatividad.");
            return;
        }

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.AddCorrelatividadAsync(
                idMateria,
                new CrearCorrelatividadRequest(editor.RequisitoSeleccionado.Valor.Id, editor.TipoRequisito),
                Token);

            await CargarCorrelatividadesAsync(editor);
            editor.RequisitoSeleccionado = null;
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
    private async Task QuitarCorrelatividadAsync(CorrelatividadDto? correlatividad)
    {
        var editor = Editor;
        if (correlatividad is null || editor?.Id is not int idMateria)
            return;

        var confirmado = _confirmation.Confirm(
            "Quitar correlatividad",
            $"¿Quitar la correlatividad con {correlatividad.MateriaRequisitoNombre}?");

        if (!confirmado)
            return;

        IsBusy = true;
        SetError(null);

        try
        {
            await _api.DeleteCorrelatividadAsync(idMateria, correlatividad.Id, Token);
            await CargarCorrelatividadesAsync(editor);
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
            var materias = await _api.GetAllAsync(null, ct);

            _materias = materias.ToList();
            BuildRows();
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
            var carreras = await _carrerasApi.GetAllAsync(ct);

            _carreras = carreras.ToList();

            CarrerasFiltro.Clear();
            CarrerasFiltro.Add(new OpcionSeleccion<CarreraDto?>(null, "Todas las carreras"));
            foreach (var carrera in _carreras)
                CarrerasFiltro.Add(new OpcionSeleccion<CarreraDto?>(carrera, carrera.Nombre));

            SeleccionarCarreraContexto();
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
    }

    private async Task CargarCorrelatividadesAsync(MateriaEditorViewModel editor)
    {
        if (editor.Id is not int idMateria)
            return;

        try
        {
            var correlatividades = await _api.GetCorrelatividadesAsync(idMateria, Token);

            if (ReferenceEquals(Editor, editor))
                editor.SetCorrelatividades(correlatividades);
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
            SetError("No se pudieron cargar las correlatividades.");
        }
    }

    private void BuildRows()
    {
        var nombresCarrera = _carreras.ToDictionary(carrera => carrera.Id, carrera => carrera.Nombre);

        _todas.Clear();
        foreach (var materia in _materias)
        {
            var carreraNombre = nombresCarrera.GetValueOrDefault(materia.IdCarrera, string.Empty);
            _todas.Add(new MateriaRow(materia, carreraNombre));
        }
    }

    private void SeleccionarCarreraContexto()
    {
        if (CarrerasFiltro.Count == 0)
            return;

        CarreraFiltro = CarreraActivaId is int idCarrera
            ? CarrerasFiltro.FirstOrDefault(opcion => opcion.Valor?.Id == idCarrera) ?? CarrerasFiltro[0]
            : CarrerasFiltro[0];
    }

    private void Rebuild()
    {
        var selectedId = SelectedMateria?.Id;
        var term = SearchText.Trim();
        var idCarrera = CarreraFiltro?.Valor?.Id;

        var visibles = _todas.Where(row =>
            (!SoloActivas || row.Activa) &&
            (idCarrera is null || row.Materia.IdCarrera == idCarrera) &&
            (term.Length == 0 ||
             row.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             row.Codigo.Contains(term, StringComparison.OrdinalIgnoreCase)));

        Materias.Clear();
        foreach (var materia in visibles)
            Materias.Add(materia);

        SelectedMateria = Materias.FirstOrDefault(materia => materia.Id == selectedId);

        TotalCount = _todas.Count;
        VisibleCount = Materias.Count;
        MostrarVacio = !IsBusy && Materias.Count == 0;
    }
}
