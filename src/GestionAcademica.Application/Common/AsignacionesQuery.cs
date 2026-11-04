using GestionAcademica.Domain.Entities;

namespace GestionAcademica.Application.Common;

/// <summary>Filtros comunes sobre las asignaciones docentes vigentes.</summary>
public static class AsignacionesQuery
{
    public static IQueryable<AsignacionDocente> VigentesDe(this IQueryable<AsignacionDocente> asignaciones, int idPersona)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return asignaciones.Where(a =>
            a.IdPersona == idPersona
            && a.Activo
            && a.FechaDesde <= hoy
            && (a.FechaHasta == null || a.FechaHasta >= hoy));
    }
}
