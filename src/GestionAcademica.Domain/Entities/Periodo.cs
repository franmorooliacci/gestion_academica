using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `periodos`. La columna `año` se expone como Anio.</summary>
public sealed class Periodo
{
    public int IdPeriodo { get; set; }
    public short Anio { get; set; }
    public TipoPeriodo Tipo { get; set; } = TipoPeriodo.PrimerCuatrimestre;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public bool InscripcionComisionesAbierta { get; set; }
    public bool InscripcionFinalesAbierta { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Comision> Comisiones { get; set; } = new List<Comision>();
    public ICollection<Examen> Examenes { get; set; } = new List<Examen>();
}
