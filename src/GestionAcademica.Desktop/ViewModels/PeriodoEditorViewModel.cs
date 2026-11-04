using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Formulario de alta/edición de período y apertura de ventanas.</summary>
public partial class PeriodoEditorViewModel : EditorViewModelBase
{
    public PeriodoEditorViewModel(PeriodoDto? periodo)
    {
        EsAlta = periodo is null;
        Id = periodo?.Id;
        Titulo = EsAlta ? "Nuevo período" : "Editar período";

        if (periodo is not null)
        {
            Anio = periodo.Anio;
            Tipo = periodo.Tipo;
            FechaInicio = periodo.FechaInicio.ToDateTime(TimeOnly.MinValue);
            FechaFin = periodo.FechaFin.ToDateTime(TimeOnly.MinValue);
            InscripcionComisionesAbierta = periodo.InscripcionComisionesAbierta;
            InscripcionFinalesAbierta = periodo.InscripcionFinalesAbierta;
            Activo = periodo.Activo;
        }
        else
        {
            Anio = DateTime.Today.Year;
            FechaInicio = DateTime.Today;
            FechaFin = DateTime.Today.AddMonths(4);
        }
    }

    public int? Id { get; }

    public IReadOnlyList<TipoPeriodo> Tipos { get; } = Enum.GetValues<TipoPeriodo>();

    [ObservableProperty]
    [Required(ErrorMessage = "El año es obligatorio.")]
    [Range(2000, 2100, ErrorMessage = "El año debe estar entre 2000 y 2100.")]
    private double? _anio;

    [ObservableProperty]
    private TipoPeriodo _tipo = TipoPeriodo.PrimerCuatrimestre;

    [ObservableProperty]
    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    private DateTime? _fechaInicio;

    [ObservableProperty]
    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    private DateTime? _fechaFin;

    [ObservableProperty]
    private bool _inscripcionComisionesAbierta;

    [ObservableProperty]
    private bool _inscripcionFinalesAbierta;

    [ObservableProperty]
    private bool _activo = true;

    partial void OnAnioChanged(double? value) => Revalidate(value, nameof(Anio));

    partial void OnFechaInicioChanged(DateTime? value) => Revalidate(value, nameof(FechaInicio));

    partial void OnFechaFinChanged(DateTime? value) => Revalidate(value, nameof(FechaFin));

    protected override string? ValidateAdicional()
    {
        if (FechaInicio.HasValue && FechaFin.HasValue && FechaFin.Value.Date < FechaInicio.Value.Date)
            return "La fecha de fin no puede ser anterior a la de inicio.";

        return null;
    }

    public CrearPeriodoRequest ToCreateRequest() => new(
        (short)(Anio ?? 0),
        Tipo,
        DateOnly.FromDateTime(FechaInicio!.Value),
        DateOnly.FromDateTime(FechaFin!.Value));

    public ActualizarPeriodoRequest ToUpdateRequest() => new(
        (short)(Anio ?? 0),
        Tipo,
        DateOnly.FromDateTime(FechaInicio!.Value),
        DateOnly.FromDateTime(FechaFin!.Value),
        Activo);

    public ActualizarVentanasRequest ToVentanasRequest()
        => new(InscripcionComisionesAbierta, InscripcionFinalesAbierta);
}
