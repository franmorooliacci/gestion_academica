using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Formulario de alta/edición de carrera. Solo arma los requests del contrato.</summary>
public partial class CarreraEditorViewModel : EditorViewModelBase
{
    public CarreraEditorViewModel(CarreraDto? carrera)
    {
        EsAlta = carrera is null;
        Id = carrera?.Id;
        Titulo = EsAlta ? "Nueva carrera" : "Editar carrera";

        if (carrera is not null)
        {
            Nombre = carrera.Nombre;
            Tipo = carrera.Tipo;
            TituloOtorgado = carrera.TituloOtorgado;
            DuracionAnios = carrera.DuracionAnios;
            Activa = carrera.Activa;
        }
    }

    public int? Id { get; }

    public IReadOnlyList<TipoCarrera> Tipos { get; } = Enum.GetValues<TipoCarrera>();

    [ObservableProperty]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private TipoCarrera _tipo = TipoCarrera.Grado;

    [ObservableProperty]
    [Required(ErrorMessage = "El título otorgado es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El título no puede superar los 120 caracteres.")]
    private string _tituloOtorgado = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "La duración es obligatoria.")]
    [Range(1, 100, ErrorMessage = "La duración debe estar entre 1 y 100 años.")]
    private double? _duracionAnios = 4;

    [ObservableProperty]
    private bool _activa = true;

    partial void OnNombreChanged(string value) => Revalidate(value, nameof(Nombre));

    partial void OnTituloOtorgadoChanged(string value) => Revalidate(value, nameof(TituloOtorgado));

    partial void OnDuracionAniosChanged(double? value) => Revalidate(value, nameof(DuracionAnios));

    public CrearCarreraRequest ToCreateRequest() => new(
        Nombre.Trim(),
        Tipo,
        TituloOtorgado.Trim(),
        (short)(DuracionAnios ?? 0));

    public ActualizarCarreraRequest ToUpdateRequest() => new(
        Nombre.Trim(),
        Tipo,
        TituloOtorgado.Trim(),
        (short)(DuracionAnios ?? 0),
        Activa);
}
