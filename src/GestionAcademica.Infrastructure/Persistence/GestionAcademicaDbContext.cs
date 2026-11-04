using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Shared.Contracts.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GestionAcademica.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core (Database First). Las columnas `año` se mapean a propiedades ASCII
/// mediante HasColumnName; el resto se resuelve con la convención snake_case.
/// </summary>
public sealed class GestionAcademicaDbContext : DbContext, IApplicationDbContext
{
    public GestionAcademicaDbContext(DbContextOptions<GestionAcademicaDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<PersonaRol> PersonasRoles => Set<PersonaRol>();
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<InscripcionCarrera> InscripcionesCarrera => Set<InscripcionCarrera>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Correlatividad> Correlatividades => Set<Correlatividad>();
    public DbSet<AsignacionDocente> AsignacionesDocentes => Set<AsignacionDocente>();
    public DbSet<Periodo> Periodos => Set<Periodo>();
    public DbSet<Comision> Comisiones => Set<Comision>();
    public DbSet<InscripcionComision> InscripcionesComision => Set<InscripcionComision>();
    public DbSet<Examen> Examenes => Set<Examen>();
    public DbSet<InscripcionExamen> InscripcionesExamen => Set<InscripcionExamen>();
    public DbSet<Certificado> Certificados => Set<Certificado>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // PKs (la convención de EF solo detecta `Id` o `<Tipo>Id`; aquí usamos el prefijo id_)
        builder.Entity<Rol>().HasKey(r => r.IdRol);
        builder.Entity<Persona>().HasKey(p => p.IdPersona);
        builder.Entity<Carrera>().HasKey(c => c.IdCarrera);
        builder.Entity<InscripcionCarrera>().HasKey(ic => ic.IdInscripcionCarrera);
        builder.Entity<Materia>().HasKey(m => m.IdMateria);
        builder.Entity<Correlatividad>().HasKey(c => c.IdCorrelatividad);
        builder.Entity<AsignacionDocente>().HasKey(a => a.IdAsignacion);
        builder.Entity<Periodo>().HasKey(p => p.IdPeriodo);
        builder.Entity<Comision>().HasKey(c => c.IdComision);
        builder.Entity<InscripcionComision>().HasKey(i => i.IdInscripcionComision);
        builder.Entity<Examen>().HasKey(e => e.IdExamen);
        builder.Entity<InscripcionExamen>().HasKey(i => i.IdInscripcionExamen);
        builder.Entity<Certificado>().HasKey(c => c.IdCertificado);
        builder.Entity<Certificado>().HasIndex(c => c.CodigoValidacion).IsUnique();
        builder.Entity<Certificado>().Property(c => c.CodigoValidacion).HasMaxLength(32).IsRequired();
        builder.Entity<Certificado>().Property(c => c.RutaArchivo).HasMaxLength(255);

        // Columnas con caracteres no-ASCII (la convención snake_case produce 'anio', etc.)
        builder.Entity<Periodo>().Property(p => p.Anio).HasColumnName("año");
        builder.Entity<Carrera>().Property(c => c.DuracionAnios).HasColumnName("duracion_años");
        builder.Entity<Materia>().Property(m => m.AnioEnCarrera).HasColumnName("año_en_carrera");
        builder.Entity<InscripcionCarrera>().Property(i => i.AnioUltimaReinscripcion).HasColumnName("año_ultima_reinscripcion");

        // personas_roles: clave compuesta
        builder.Entity<PersonaRol>().HasKey(pr => new { pr.IdPersona, pr.IdRol });
        builder.Entity<PersonaRol>()
            .HasOne(pr => pr.Persona).WithMany(p => p.PersonaRoles).HasForeignKey(pr => pr.IdPersona);
        builder.Entity<PersonaRol>()
            .HasOne(pr => pr.Rol).WithMany(r => r.PersonaRoles).HasForeignKey(pr => pr.IdRol);

        // Persona
        builder.Entity<Persona>()
            .HasMany(p => p.InscripcionesCarrera).WithOne(ic => ic.Persona).HasForeignKey(ic => ic.IdPersona);
        builder.Entity<Persona>()
            .HasMany(p => p.AsignacionesDocentes).WithOne(a => a.Persona).HasForeignKey(a => a.IdPersona);

        // Carrera
        builder.Entity<Carrera>()
            .HasMany(c => c.Materias).WithOne(m => m.Carrera).HasForeignKey(m => m.IdCarrera);
        builder.Entity<Carrera>()
            .HasMany(c => c.InscripcionesCarrera).WithOne(ic => ic.Carrera).HasForeignKey(ic => ic.IdCarrera);

        // InscripcionCarrera
        builder.Entity<InscripcionCarrera>()
            .HasMany(ic => ic.InscripcionesComision).WithOne(i => i.InscripcionCarrera).HasForeignKey(i => i.IdInscripcionCarrera);
        builder.Entity<InscripcionCarrera>()
            .HasMany(ic => ic.InscripcionesExamen).WithOne(i => i.InscripcionCarrera).HasForeignKey(i => i.IdInscripcionCarrera);
        builder.Entity<InscripcionCarrera>()
            .HasMany(ic => ic.Certificados).WithOne(c => c.InscripcionCarrera).HasForeignKey(c => c.IdInscripcionCarrera);

        // Materia
        builder.Entity<Materia>()
            .HasMany(m => m.Correlatividades).WithOne(c => c.Materia).HasForeignKey(c => c.IdMateria).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Materia>()
            .HasMany(m => m.CorrelatividadesRequisito).WithOne(c => c.MateriaRequisito).HasForeignKey(c => c.IdMateriaRequisito).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Materia>()
            .HasMany(m => m.AsignacionesDocentes).WithOne(a => a.Materia).HasForeignKey(a => a.IdMateria);
        builder.Entity<Materia>()
            .HasMany(m => m.Comisiones).WithOne(c => c.Materia).HasForeignKey(c => c.IdMateria);
        builder.Entity<Materia>()
            .HasMany(m => m.Examenes).WithOne(e => e.Materia).HasForeignKey(e => e.IdMateria);

        // AsignacionDocente -> Comision (opcional)
        builder.Entity<AsignacionDocente>()
            .HasOne(a => a.Comision).WithMany(c => c.AsignacionesDocentes).HasForeignKey(a => a.IdComision).IsRequired(false);

        // Periodo
        builder.Entity<Periodo>()
            .HasMany(p => p.Comisiones).WithOne(c => c.Periodo).HasForeignKey(c => c.IdPeriodo);
        builder.Entity<Periodo>()
            .HasMany(p => p.Examenes).WithOne(e => e.Periodo).HasForeignKey(e => e.IdPeriodo);

        // Comision
        builder.Entity<Comision>()
            .HasMany(c => c.InscripcionesComision).WithOne(i => i.Comision).HasForeignKey(i => i.IdComision);

        // Examen
        builder.Entity<Examen>()
            .HasMany(e => e.InscripcionesExamen).WithOne(i => i.Examen).HasForeignKey(i => i.IdExamen);

        // Soft delete (borrado lógico)
        builder.Entity<Rol>().HasQueryFilter(r => r.Activo);
        builder.Entity<Persona>().HasQueryFilter(p => p.Activo);
        builder.Entity<PersonaRol>().HasQueryFilter(pr => pr.Activo);
        builder.Entity<Carrera>().HasQueryFilter(c => c.Activa);
        builder.Entity<Materia>().HasQueryFilter(m => m.Activa);
        builder.Entity<AsignacionDocente>().HasQueryFilter(a => a.Activo);
        builder.Entity<Periodo>().HasQueryFilter(p => p.Activo);
        builder.Entity<Comision>().HasQueryFilter(c => c.Activo);

        ApplyEnumConversions(builder);
    }

    private static void ApplyEnumConversions(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!enumType.IsEnum) continue;

                var converterType = property.ClrType == enumType
                    ? typeof(EnumMemberValueConverter<>).MakeGenericType(enumType)
                    : typeof(NullableEnumMemberValueConverter<>).MakeGenericType(enumType);

                property.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
            }
        }
    }

    private sealed class EnumMemberValueConverter<TEnum> : ValueConverter<TEnum, string> where TEnum : struct, Enum
    {
        public EnumMemberValueConverter()
            : base(v => v.ToValue(), s => EnumHelpers.ToEnum<TEnum>(s)) { }
    }

    private sealed class NullableEnumMemberValueConverter<TEnum> : ValueConverter<TEnum?, string?> where TEnum : struct, Enum
    {
        public NullableEnumMemberValueConverter()
            : base(
                v => v.HasValue ? v.Value.ToValue() : null,
                s => string.IsNullOrEmpty(s) ? null : EnumHelpers.ToEnum<TEnum>(s)) { }
    }
}
