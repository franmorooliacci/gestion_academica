using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `asignaciones_docentes`.</summary>
public sealed class AsignacionDocente
{
    public int IdAsignacion { get; set; }
    public int IdMateria { get; set; }
    public int IdPersona { get; set; }
    public int? IdComision { get; set; }
    public CargoDocente Cargo { get; set; }
    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool Activo { get; set; } = true;

    public Materia Materia { get; set; } = null!;
    public Persona Persona { get; set; } = null!;
    public Comision? Comision { get; set; }
}
