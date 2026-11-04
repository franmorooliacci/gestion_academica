using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionAcademica.Desktop.ViewModels.Infrastructure;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.ViewModels;

/// <summary>Formulario de alta/edición de mesa de examen.</summary>
public partial class ExamenEditorViewModel : EditorViewModelBase
{
    public ExamenEditorViewModel(
        ExamenDto? examen,
        IReadOnlyList<MateriaDto> materias,
        IReadOnlyList<PeriodoDto> periodos,
        int? idCarreraPredeterminada = null)
    {
        EsAlta = examen is null;
        Id = examen?.Id;
        Titulo = EsAlta ? "Nueva mesa de examen" : "Editar mesa de examen";

        var opcionesMaterias = materias
            .Select(materia => new OpcionSeleccion<int>(materia.Id, $"{materia.Codigo} · {materia.Nombre}"))
            .ToList();

        if (examen is not null && opcionesMaterias.All(opcion => opcion.Valor != examen.IdMateria))
            opcionesMaterias.Add(new OpcionSeleccion<int>(examen.IdMateria, examen.MateriaNombre));

        Materias = opcionesMaterias;

        var opcionesPeriodos = new List<OpcionSeleccion<int?>> { new(null, "Sin período") };
        opcionesPeriodos.AddRange(periodos.Select(periodo => new OpcionSeleccion<int?>(
            periodo.Id,
            EnumTextos.Periodo(periodo.Anio, periodo.Tipo))));

        Periodos = opcionesPeriodos;

        if (examen is not null)
        {
            MateriaSeleccionada = Materias.FirstOrDefault(opcion => opcion.Valor == examen.IdMateria);
            PeriodoSeleccionado = Periodos.FirstOrDefault(opcion => opcion.Valor == examen.IdPeriodo)
                ?? Periodos[0];
            Fecha = examen.Fecha.Date;
            Hora = examen.Fecha.TimeOfDay;
            Aula = examen.Aula;
        }
        else
        {
            var idMateria = materias
                .FirstOrDefault(materia => materia.IdCarrera == idCarreraPredeterminada)?.Id;

            MateriaSeleccionada = idMateria is int id
                ? Materias.FirstOrDefault(opcion => opcion.Valor == id)
                : null;
            MateriaSeleccionada ??= Materias.FirstOrDefault();
            PeriodoSeleccionado = Periodos[0];
            Fecha = DateTime.Today;
            Hora = new TimeSpan(8, 0, 0);
        }
    }

    public int? Id { get; }

    public IReadOnlyList<OpcionSeleccion<int>> Materias { get; }

    public IReadOnlyList<OpcionSeleccion<int?>> Periodos { get; }

    [ObservableProperty]
    [Required(ErrorMessage = "La materia es obligatoria.")]
    private OpcionSeleccion<int>? _materiaSeleccionada;

    [ObservableProperty]
    private OpcionSeleccion<int?>? _periodoSeleccionado;

    [ObservableProperty]
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    private DateTime? _fecha;

    [ObservableProperty]
    private TimeSpan? _hora;

    [ObservableProperty]
    [MaxLength(50, ErrorMessage = "El aula no puede superar los 50 caracteres.")]
    private string? _aula;

    partial void OnMateriaSeleccionadaChanged(OpcionSeleccion<int>? value)
        => Revalidate(value, nameof(MateriaSeleccionada));

    partial void OnFechaChanged(DateTime? value) => Revalidate(value, nameof(Fecha));

    partial void OnAulaChanged(string? value) => Revalidate(value, nameof(Aula));

    public CrearExamenRequest ToCreateRequest() => new(
        MateriaSeleccionada!.Valor,
        PeriodoSeleccionado?.Valor,
        FechaHora(),
        NullIfEmpty(Aula));

    public ActualizarExamenRequest ToUpdateRequest() => new(
        MateriaSeleccionada!.Valor,
        PeriodoSeleccionado?.Valor,
        FechaHora(),
        NullIfEmpty(Aula));

    private DateTime FechaHora()
    {
        var fecha = Fecha!.Value.Date;
        return fecha.Add(Hora ?? TimeSpan.Zero);
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
