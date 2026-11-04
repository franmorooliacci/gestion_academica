using GestionAcademica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.Interfaces;

/// <summary>
/// Abstracción del contexto de EF Core. La implementación vive en Infrastructure
/// y mapea las entidades de Domain contra el schema MySQL.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Rol> Roles { get; }
    DbSet<Persona> Personas { get; }
    DbSet<PersonaRol> PersonasRoles { get; }
    DbSet<Carrera> Carreras { get; }
    DbSet<InscripcionCarrera> InscripcionesCarrera { get; }
    DbSet<Materia> Materias { get; }
    DbSet<Correlatividad> Correlatividades { get; }
    DbSet<AsignacionDocente> AsignacionesDocentes { get; }
    DbSet<Periodo> Periodos { get; }
    DbSet<Comision> Comisiones { get; }
    DbSet<InscripcionComision> InscripcionesComision { get; }
    DbSet<Examen> Examenes { get; }
    DbSet<InscripcionExamen> InscripcionesExamen { get; }
    DbSet<Certificado> Certificados { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
