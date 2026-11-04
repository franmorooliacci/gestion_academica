using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Formulario de alta/edición de asignación docente.</summary>
public partial class AsignacionEditorViewModel : EditorViewModelBase
{
    private readonly IReadOnlyList<ComisionDto> _todasLasComisiones;

    public AsignacionEditorViewModel(
        AsignacionDocenteDto? asignacion,
        IReadOnlyList<MateriaDto> materias,
        IReadOnlyList<PersonaDto> docentes,
        IReadOnlyList<ComisionDto> comisiones,
        int? idCarreraPredeterminada = null)
    {
        EsAlta = asignacion is null;
        Id = asignacion?.Id;
        Titulo = EsAlta ? "Nueva asignación" : "Editar asignación";
        _todasLasComisiones = comisiones;

        var opcionesMaterias = materias
            .Select(materia => new OpcionSeleccion<int>(materia.Id, $"{materia.Codigo} · {materia.Nombre}"))
            .ToList();

        if (asignacion is not null && opcionesMaterias.All(opcion => opcion.Valor != asignacion.IdMateria))
            opcionesMaterias.Add(new OpcionSeleccion<int>(asignacion.IdMateria, asignacion.MateriaNombre));

        Materias = opcionesMaterias;

        var opcionesDocentes = docentes
            .Select(docente => new OpcionSeleccion<int>(docente.Id, $"{docente.Apellido}, {docente.Nombre}"))
            .ToList();

        if (asignacion is not null && opcionesDocentes.All(opcion => opcion.Valor != asignacion.IdPersona))
            opcionesDocentes.Add(new OpcionSeleccion<int>(asignacion.IdPersona, asignacion.DocenteNombre));

        Docentes = opcionesDocentes;

        var idMateriaPredeterminada = materias
            .FirstOrDefault(materia => materia.IdCarrera == idCarreraPredeterminada)?.Id;

        MateriaSeleccionada = asignacion is null
            ? Materias.FirstOrDefault(opcion => opcion.Valor == idMateriaPredeterminada)
              ?? Materias.FirstOrDefault()
            : Materias.FirstOrDefault(opcion => opcion.Valor == asignacion.IdMateria);

        DocenteSeleccionado = asignacion is null
            ? Docentes.FirstOrDefault()
            : Docentes.FirstOrDefault(opcion => opcion.Valor == asignacion.IdPersona);

        if (asignacion is not null)
        {
            ComisionSeleccionada = Comisiones.FirstOrDefault(opcion => opcion.Valor == asignacion.IdComision)
                ?? Comisiones.FirstOrDefault();
            Cargo = asignacion.Cargo;
            FechaDesde = asignacion.FechaDesde.ToDateTime(TimeOnly.MinValue);
            FechaHasta = asignacion.FechaHasta?.ToDateTime(TimeOnly.MinValue);
            Activo = asignacion.Activo;
        }
        else
        {
            FechaDesde = DateTime.Today;
        }
    }

    public int? Id { get; }

    public IReadOnlyList<OpcionSeleccion<int>> Materias { get; }

    public IReadOnlyList<OpcionSeleccion<int>> Docentes { get; }

    public ObservableCollection<OpcionSeleccion<int?>> Comisiones { get; } = [];

    public IReadOnlyList<CargoDocente> Cargos { get; } = Enum.GetValues<CargoDocente>();

    [ObservableProperty]
    [Required(ErrorMessage = "La materia es obligatoria.")]
    private OpcionSeleccion<int>? _materiaSeleccionada;

    [ObservableProperty]
    [Required(ErrorMessage = "El docente es obligatorio.")]
    private OpcionSeleccion<int>? _docenteSeleccionado;

    [ObservableProperty]
    private OpcionSeleccion<int?>? _comisionSeleccionada;

    [ObservableProperty]
    private CargoDocente _cargo = CargoDocente.ProfesorAdjunto;

    [ObservableProperty]
    [Required(ErrorMessage = "La fecha desde es obligatoria.")]
    private DateTime? _fechaDesde;

    [ObservableProperty]
    private DateTime? _fechaHasta;

    [ObservableProperty]
    private bool _activo = true;

    partial void OnMateriaSeleccionadaChanged(OpcionSeleccion<int>? value)
    {
        Revalidate(value, nameof(MateriaSeleccionada));
        RebuildComisiones();
    }

    partial void OnDocenteSeleccionadoChanged(OpcionSeleccion<int>? value)
        => Revalidate(value, nameof(DocenteSeleccionado));

    partial void OnFechaDesdeChanged(DateTime? value) => Revalidate(value, nameof(FechaDesde));

    protected override string? ValidateAdicional()
    {
        if (FechaDesde.HasValue && FechaHasta.HasValue && FechaHasta.Value.Date < FechaDesde.Value.Date)
            return "La fecha hasta no puede ser anterior a la fecha desde.";

        return null;
    }

    public CrearAsignacionDocenteRequest ToCreateRequest() => new(
        MateriaSeleccionada!.Valor,
        DocenteSeleccionado!.Valor,
        ComisionSeleccionada?.Valor,
        Cargo,
        DateOnly.FromDateTime(FechaDesde!.Value),
        FechaHasta is null ? null : DateOnly.FromDateTime(FechaHasta.Value));

    public ActualizarAsignacionDocenteRequest ToUpdateRequest() => new(
        MateriaSeleccionada!.Valor,
        DocenteSeleccionado!.Valor,
        ComisionSeleccionada?.Valor,
        Cargo,
        DateOnly.FromDateTime(FechaDesde!.Value),
        FechaHasta is null ? null : DateOnly.FromDateTime(FechaHasta.Value),
        Activo);

    private void RebuildComisiones()
    {
        var comisionSeleccionada = ComisionSeleccionada?.Valor;
        var idMateria = MateriaSeleccionada?.Valor;

        Comisiones.Clear();
        Comisiones.Add(new OpcionSeleccion<int?>(null, "Sin comisión (toda la materia)"));

        if (idMateria is not null)
        {
            foreach (var comision in _todasLasComisiones.Where(comision => comision.IdMateria == idMateria))
                Comisiones.Add(new OpcionSeleccion<int?>(comision.Id, comision.Codigo));
        }

        ComisionSeleccionada = Comisiones.FirstOrDefault(opcion => opcion.Valor == comisionSeleccionada)
            ?? Comisiones[0];
    }
}
