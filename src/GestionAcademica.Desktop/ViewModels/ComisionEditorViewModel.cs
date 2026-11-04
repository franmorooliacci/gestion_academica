using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Formulario de alta/edición de comisión.</summary>
public partial class ComisionEditorViewModel : EditorViewModelBase
{
    public ComisionEditorViewModel(
        ComisionDto? comision,
        IReadOnlyList<MateriaDto> materias,
        IReadOnlyList<PeriodoDto> periodos,
        int? idCarreraPredeterminada = null)
    {
        EsAlta = comision is null;
        Id = comision?.Id;
        Titulo = EsAlta ? "Nueva comisión" : "Editar comisión";

        Materias = materias
            .Select(materia => new OpcionSeleccion<MateriaDto>(materia, $"{materia.Codigo} · {materia.Nombre}"))
            .ToList();

        Periodos = periodos
            .Select(periodo => new OpcionSeleccion<PeriodoDto>(
                periodo,
                EnumTextos.Periodo(periodo.Anio, periodo.Tipo)))
            .ToList();

        if (comision is not null)
        {
            MateriaSeleccionada = Materias.FirstOrDefault(opcion => opcion.Valor.Id == comision.IdMateria);
            PeriodoSeleccionado = Periodos.FirstOrDefault(opcion => opcion.Valor.Id == comision.IdPeriodo);
            Codigo = comision.Codigo;
            Turno = comision.Turno;
            ModalidadDictado = comision.ModalidadDictado;
            CupoMaximo = comision.CupoMaximo;
            ListaEsperaMaxima = comision.ListaEsperaMaxima;
            Activo = comision.Activo;
        }
        else
        {
            MateriaSeleccionada = Materias.FirstOrDefault(
                    opcion => opcion.Valor.IdCarrera == idCarreraPredeterminada)
                ?? Materias.FirstOrDefault();
            PeriodoSeleccionado = Periodos.FirstOrDefault();
        }
    }

    public int? Id { get; }

    public IReadOnlyList<OpcionSeleccion<MateriaDto>> Materias { get; }

    public IReadOnlyList<OpcionSeleccion<PeriodoDto>> Periodos { get; }

    public IReadOnlyList<Turno> Turnos { get; } = Enum.GetValues<Turno>();

    public IReadOnlyList<ModalidadDictado> Modalidades { get; } = Enum.GetValues<ModalidadDictado>();

    [ObservableProperty]
    [Required(ErrorMessage = "La materia es obligatoria.")]
    private OpcionSeleccion<MateriaDto>? _materiaSeleccionada;

    [ObservableProperty]
    [Required(ErrorMessage = "El período es obligatorio.")]
    private OpcionSeleccion<PeriodoDto>? _periodoSeleccionado;

    [ObservableProperty]
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(10, ErrorMessage = "El código no puede superar los 10 caracteres.")]
    private string _codigo = string.Empty;

    [ObservableProperty]
    private Turno _turno = Turno.Maniana;

    [ObservableProperty]
    private ModalidadDictado _modalidadDictado = ModalidadDictado.Presencial;

    [ObservableProperty]
    [Range(1, 10000, ErrorMessage = "El cupo debe ser mayor que cero.")]
    private double? _cupoMaximo;

    [ObservableProperty]
    [Range(0, 10000, ErrorMessage = "La lista de espera no puede ser negativa.")]
    private double? _listaEsperaMaxima;

    [ObservableProperty]
    private bool _activo = true;

    partial void OnMateriaSeleccionadaChanged(OpcionSeleccion<MateriaDto>? value)
        => Revalidate(value, nameof(MateriaSeleccionada));

    partial void OnPeriodoSeleccionadoChanged(OpcionSeleccion<PeriodoDto>? value)
        => Revalidate(value, nameof(PeriodoSeleccionado));

    partial void OnCodigoChanged(string value) => Revalidate(value, nameof(Codigo));

    partial void OnCupoMaximoChanged(double? value) => Revalidate(value, nameof(CupoMaximo));

    partial void OnListaEsperaMaximaChanged(double? value) => Revalidate(value, nameof(ListaEsperaMaxima));

    public CrearComisionRequest ToCreateRequest() => new(
        MateriaSeleccionada!.Valor.Id,
        PeriodoSeleccionado!.Valor.Id,
        Codigo.Trim(),
        Turno,
        ModalidadDictado,
        CupoMaximo is null ? null : (int)CupoMaximo.Value,
        ListaEsperaMaxima is null ? null : (int)ListaEsperaMaxima.Value);

    public ActualizarComisionRequest ToUpdateRequest() => new(
        MateriaSeleccionada!.Valor.Id,
        PeriodoSeleccionado!.Valor.Id,
        Codigo.Trim(),
        Turno,
        ModalidadDictado,
        CupoMaximo is null ? null : (int)CupoMaximo.Value,
        ListaEsperaMaxima is null ? null : (int)ListaEsperaMaxima.Value,
        Activo);
}
