using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Formulario de alta/edición de materia y sus correlatividades.</summary>
public partial class MateriaEditorViewModel : EditorViewModelBase
{
    public MateriaEditorViewModel(
        MateriaDto? materia,
        IReadOnlyList<CarreraDto> carreras,
        IReadOnlyList<MateriaDto> materias,
        int? idCarreraPredeterminada = null)
    {
        EsAlta = materia is null;
        Id = materia?.Id;
        Titulo = EsAlta ? "Nueva materia" : "Editar materia";

        Carreras = carreras
            .Select(carrera => new OpcionSeleccion<CarreraDto>(carrera, carrera.Nombre))
            .ToList();

        Requisitos = materias
            .Where(otra => otra.Id != materia?.Id)
            .Select(otra => new OpcionSeleccion<MateriaDto>(otra, $"{otra.Codigo} · {otra.Nombre}"))
            .ToList();

        if (materia is not null)
        {
            CarreraSeleccionada = Carreras.FirstOrDefault(opcion => opcion.Valor.Id == materia.IdCarrera);
            Codigo = materia.Codigo;
            Nombre = materia.Nombre;
            AnioEnCarrera = materia.AnioEnCarrera;
            RegimenCursada = materia.RegimenCursada;
            CargaHorariaTotal = materia.CargaHorariaTotal;
            Activa = materia.Activa;
        }
        else
        {
            CarreraSeleccionada = Carreras.FirstOrDefault(opcion => opcion.Valor.Id == idCarreraPredeterminada)
                ?? Carreras.FirstOrDefault();
        }
    }

    public int? Id { get; }

    public IReadOnlyList<OpcionSeleccion<CarreraDto>> Carreras { get; }

    public IReadOnlyList<OpcionSeleccion<MateriaDto>> Requisitos { get; }

    public IReadOnlyList<RegimenCursada> Regimenes { get; } = Enum.GetValues<RegimenCursada>();

    public IReadOnlyList<TipoRequisito> TiposRequisito { get; } = Enum.GetValues<TipoRequisito>();

    public ObservableCollection<CorrelatividadDto> Correlatividades { get; } = [];

    [ObservableProperty]
    [Required(ErrorMessage = "La carrera es obligatoria.")]
    private OpcionSeleccion<CarreraDto>? _carreraSeleccionada;

    [ObservableProperty]
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    private string _codigo = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    private string _nombre = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "El año en la carrera es obligatorio.")]
    [Range(1, 20, ErrorMessage = "El año debe estar entre 1 y 20.")]
    private double? _anioEnCarrera = 1;

    [ObservableProperty]
    private RegimenCursada _regimenCursada = RegimenCursada.Cuatrimestral;

    [ObservableProperty]
    [Required(ErrorMessage = "La carga horaria es obligatoria.")]
    [Range(1, 5000, ErrorMessage = "La carga horaria debe estar entre 1 y 5000 horas.")]
    private double? _cargaHorariaTotal = 64;

    [ObservableProperty]
    private bool _activa = true;

    [ObservableProperty]
    private OpcionSeleccion<MateriaDto>? _requisitoSeleccionado;

    [ObservableProperty]
    private TipoRequisito _tipoRequisito = TipoRequisito.Regularizada;

    [ObservableProperty]
    private bool _tieneCorrelatividades;

    partial void OnCarreraSeleccionadaChanged(OpcionSeleccion<CarreraDto>? value)
        => Revalidate(value, nameof(CarreraSeleccionada));

    partial void OnCodigoChanged(string value) => Revalidate(value, nameof(Codigo));

    partial void OnNombreChanged(string value) => Revalidate(value, nameof(Nombre));

    partial void OnAnioEnCarreraChanged(double? value) => Revalidate(value, nameof(AnioEnCarrera));

    partial void OnCargaHorariaTotalChanged(double? value) => Revalidate(value, nameof(CargaHorariaTotal));

    public void SetCorrelatividades(IEnumerable<CorrelatividadDto> correlatividades)
    {
        Correlatividades.Clear();
        foreach (var correlatividad in correlatividades)
            Correlatividades.Add(correlatividad);

        TieneCorrelatividades = Correlatividades.Count > 0;
    }

    public CrearMateriaRequest ToCreateRequest() => new(
        CarreraSeleccionada!.Valor.Id,
        Codigo.Trim(),
        Nombre.Trim(),
        (short)(AnioEnCarrera ?? 0),
        RegimenCursada,
        (int)(CargaHorariaTotal ?? 0));

    public ActualizarMateriaRequest ToUpdateRequest() => new(
        CarreraSeleccionada!.Valor.Id,
        Codigo.Trim(),
        Nombre.Trim(),
        (short)(AnioEnCarrera ?? 0),
        RegimenCursada,
        (int)(CargaHorariaTotal ?? 0),
        Activa);
}
