using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Infrastructure.Seeding;

/// <summary>Datos mínimos de desarrollo: roles y un usuario administrativo.</summary>
public sealed class DbSeeder
{
    public const string AdministrativoUsername = "administrativo";
    public const string AdministrativoPassword = "Administrativo1234!";

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public DbSeeder(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedAdministrativoAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        foreach (var nombre in new[] { AppRoles.Administrativo, AppRoles.Alumno, AppRoles.Profesor })
        {
            if (!await _db.Roles.IgnoreQueryFilters().AnyAsync(r => r.Nombre == nombre, ct))
            {
                _db.Roles.Add(new Rol { Nombre = nombre });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedAdministrativoAsync(CancellationToken ct)
    {
        var administrativo = await _db.Personas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Username == AdministrativoUsername, ct);

        if (administrativo is null)
        {
            administrativo = new Persona
            {
                Nombre = "Administrativo",
                Apellido = "Sistema",
                Dni = "00000000",
                Sexo = Sexo.PrefieroNoDecir,
                FechaNacimiento = new DateOnly(1970, 1, 1),
                Email = "administrativo@gestionacademica.local",
                Username = AdministrativoUsername,
                PasswordHash = _hasher.Hash(AdministrativoPassword),
                Activo = true,
                FechaRegistro = DateTime.UtcNow,
            };
            _db.Personas.Add(administrativo);
            await _db.SaveChangesAsync(ct);
        }

        var rolAdministrativo = await _db.Roles.IgnoreQueryFilters()
            .FirstAsync(r => r.Nombre == AppRoles.Administrativo, ct);

        var yaTieneRol = await _db.PersonasRoles.IgnoreQueryFilters()
            .AnyAsync(pr => pr.IdPersona == administrativo.IdPersona && pr.IdRol == rolAdministrativo.IdRol, ct);

        if (!yaTieneRol)
        {
            _db.PersonasRoles.Add(new PersonaRol
            {
                IdPersona = administrativo.IdPersona,
                IdRol = rolAdministrativo.IdRol,
                Activo = true,
            });
            await _db.SaveChangesAsync(ct);
        }
    }
}
