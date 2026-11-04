using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetPersonasQuery(string? Search) : IRequest<List<PersonaDto>>;

public sealed class GetPersonasQueryHandler : IRequestHandler<GetPersonasQuery, List<PersonaDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPersonasQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<PersonaDto>> Handle(GetPersonasQuery request, CancellationToken ct)
    {
        var query = _db.Personas.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.PersonaRoles).ThenInclude(pr => pr.Rol)
            .Include(p => p.InscripcionesCarrera).ThenInclude(ic => ic.Carrera)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p =>
                p.Nombre.Contains(s) || p.Apellido.Contains(s) ||
                p.Dni.Contains(s) || p.Username.Contains(s) || p.Email.Contains(s));
        }

        var personas = await query.OrderBy(p => p.Apellido).ThenBy(p => p.Nombre).ToListAsync(ct);
        return personas.Select(PersonaMapper.ToDto).ToList();
    }
}

internal static class PersonaMapper
{
    public static PersonaDto ToDto(Persona p) => new(
        p.IdPersona,
        p.Nombre,
        p.Apellido,
        p.Dni,
        p.Sexo,
        p.FechaNacimiento,
        p.Telefono,
        p.Domicilio,
        p.Email,
        p.Username,
        p.Activo,
        p.PersonaRoles
            .Where(pr => pr.Activo && pr.Rol is not null)
            .Select(pr => new RolDto(pr.Rol.IdRol, pr.Rol.Nombre))
            .DistinctBy(r => r.Id)
            .ToList(),
        p.InscripcionesCarrera
            .Where(ic => ic.Carrera is not null)
            .Select(ic => new CarreraActivaDto(ic.IdInscripcionCarrera, ic.IdCarrera, ic.Carrera.Nombre, ic.Estado))
            .ToList());
}

public sealed record GetPersonaByIdQuery(int Id) : IRequest<PersonaDto>;

public sealed class GetPersonaByIdQueryHandler : IRequestHandler<GetPersonaByIdQuery, PersonaDto>
{
    private readonly IApplicationDbContext _db;
    public GetPersonaByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PersonaDto> Handle(GetPersonaByIdQuery request, CancellationToken ct)
    {
        var persona = await _db.Personas.IgnoreQueryFilters().AsNoTracking()
            .Include(p => p.PersonaRoles).ThenInclude(pr => pr.Rol)
            .Include(p => p.InscripcionesCarrera).ThenInclude(ic => ic.Carrera)
            .FirstOrDefaultAsync(p => p.IdPersona == request.Id, ct)
            ?? throw new NotFoundException($"persona {request.Id}");
        return PersonaMapper.ToDto(persona);
    }
}

public sealed record CreatePersonaCommand(CrearPersonaRequest Request) : IRequest<PersonaDto>;

public sealed class CreatePersonaCommandHandler : IRequestHandler<CreatePersonaCommand, PersonaDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    public CreatePersonaCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<PersonaDto> Handle(CreatePersonaCommand command, CancellationToken ct)
    {
        var req = command.Request;
        await EnsureUniquenessAsync(req.Dni, req.Email, req.Username, null, ct);

        var idRolAdministrativo = await RolAdministrativo.GetIdAsync(_db, ct);
        RoleAssignmentPolicy.ValidarAlta(req.IdsRoles, idRolAdministrativo);

        var persona = new Persona
        {
            Nombre = req.Nombre,
            Apellido = req.Apellido,
            Dni = req.Dni,
            Sexo = req.Sexo,
            FechaNacimiento = req.FechaNacimiento,
            Telefono = req.Telefono,
            Domicilio = req.Domicilio,
            Email = req.Email,
            Username = req.Username,
            PasswordHash = _hasher.Hash(req.Password),
            Activo = true,
            FechaRegistro = DateTime.UtcNow,
        };
        _db.Personas.Add(persona);
        await _db.SaveChangesAsync(ct);

        await SyncRolesAsync(persona.IdPersona, req.IdsRoles, ct);

        return await LoadDtoAsync(persona.IdPersona, ct);
    }

    private async Task EnsureUniquenessAsync(string dni, string email, string username, int? excludeId, CancellationToken ct)
    {
        if (await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.Dni == dni && (excludeId == null || p.IdPersona != excludeId), ct))
            throw new ReglaDeNegocioException($"Ya existe una persona con DNI '{dni}'.");
        if (await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.Email == email && (excludeId == null || p.IdPersona != excludeId), ct))
            throw new ReglaDeNegocioException($"Ya existe una persona con email '{email}'.");
        if (await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.Username == username && (excludeId == null || p.IdPersona != excludeId), ct))
            throw new ReglaDeNegocioException($"Ya existe una persona con usuario '{username}'.");
    }

    private async Task SyncRolesAsync(int idPersona, IReadOnlyList<int>? idsRoles, CancellationToken ct)
    {
        var target = (idsRoles ?? Array.Empty<int>()).Distinct().ToHashSet();
        var existentes = await _db.PersonasRoles.IgnoreQueryFilters()
            .Where(pr => pr.IdPersona == idPersona)
            .ToListAsync(ct);

        foreach (var pr in existentes)
        {
            var activo = target.Contains(pr.IdRol);
            if (pr.Activo != activo) pr.Activo = activo;
        }

        var existentesIds = existentes.Select(pr => pr.IdRol).ToHashSet();
        foreach (var idRol in target.Where(id => !existentesIds.Contains(id)))
        {
            if (!await _db.Roles.IgnoreQueryFilters().AnyAsync(r => r.IdRol == idRol, ct))
                throw new NotFoundException($"rol {idRol}");
            _db.PersonasRoles.Add(new PersonaRol { IdPersona = idPersona, IdRol = idRol, Activo = true });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<PersonaDto> LoadDtoAsync(int idPersona, CancellationToken ct)
    {
        var persona = await _db.Personas.IgnoreQueryFilters().AsNoTracking()
            .Include(p => p.PersonaRoles).ThenInclude(pr => pr.Rol)
            .Include(p => p.InscripcionesCarrera).ThenInclude(ic => ic.Carrera)
            .FirstAsync(p => p.IdPersona == idPersona, ct);
        return PersonaMapper.ToDto(persona);
    }
}

public sealed record UpdatePersonaCommand(int Id, ActualizarPersonaRequest Request) : IRequest<PersonaDto>;

public sealed class UpdatePersonaCommandHandler : IRequestHandler<UpdatePersonaCommand, PersonaDto>
{
    private readonly IApplicationDbContext _db;
    public UpdatePersonaCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PersonaDto> Handle(UpdatePersonaCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var persona = await _db.Personas.IgnoreQueryFilters()
            .Include(p => p.PersonaRoles).ThenInclude(pr => pr.Rol)
            .FirstOrDefaultAsync(p => p.IdPersona == command.Id, ct)
            ?? throw new NotFoundException($"persona {command.Id}");

        var idRolAdministrativo = await RolAdministrativo.GetIdAsync(_db, ct);
        var rolesActuales = persona.PersonaRoles
            .Where(pr => pr.Activo && pr.Rol is not null)
            .Select(pr => pr.IdRol)
            .ToHashSet();
        var tieneRolAdministrativo = persona.PersonaRoles
            .Any(pr => pr.Activo && pr.Rol?.Nombre == AppRoles.Administrativo);

        RoleAssignmentPolicy.ValidarActualizacion(
            tieneRolAdministrativo, req.IdsRoles, rolesActuales, idRolAdministrativo);

        if (await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.Dni == req.Dni && p.IdPersona != command.Id, ct))
            throw new ReglaDeNegocioException($"Ya existe una persona con DNI '{req.Dni}'.");
        if (await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.Email == req.Email && p.IdPersona != command.Id, ct))
            throw new ReglaDeNegocioException($"Ya existe una persona con email '{req.Email}'.");
        if (await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.Username == req.Username && p.IdPersona != command.Id, ct))
            throw new ReglaDeNegocioException($"Ya existe una persona con usuario '{req.Username}'.");

        persona.Nombre = req.Nombre;
        persona.Apellido = req.Apellido;
        persona.Dni = req.Dni;
        persona.Sexo = req.Sexo;
        persona.FechaNacimiento = req.FechaNacimiento;
        persona.Telefono = req.Telefono;
        persona.Domicilio = req.Domicilio;
        persona.Email = req.Email;
        persona.Username = req.Username;

        await _db.SaveChangesAsync(ct);

        if (req.IdsRoles is not null)
            await SyncRolesAsync(persona.IdPersona, req.IdsRoles, ct);

        var updated = await _db.Personas.IgnoreQueryFilters().AsNoTracking()
            .Include(p => p.PersonaRoles).ThenInclude(pr => pr.Rol)
            .Include(p => p.InscripcionesCarrera).ThenInclude(ic => ic.Carrera)
            .FirstAsync(p => p.IdPersona == persona.IdPersona, ct);
        return PersonaMapper.ToDto(updated);
    }

    private async Task SyncRolesAsync(int idPersona, IReadOnlyList<int>? idsRoles, CancellationToken ct)
    {
        var target = (idsRoles ?? Array.Empty<int>()).Distinct().ToHashSet();
        var existentes = await _db.PersonasRoles.IgnoreQueryFilters()
            .Where(pr => pr.IdPersona == idPersona)
            .ToListAsync(ct);

        foreach (var pr in existentes)
        {
            var activo = target.Contains(pr.IdRol);
            if (pr.Activo != activo) pr.Activo = activo;
        }

        var existentesIds = existentes.Select(pr => pr.IdRol).ToHashSet();
        foreach (var idRol in target.Where(id => !existentesIds.Contains(id)))
        {
            if (!await _db.Roles.IgnoreQueryFilters().AnyAsync(r => r.IdRol == idRol, ct))
                throw new NotFoundException($"rol {idRol}");
            _db.PersonasRoles.Add(new PersonaRol { IdPersona = idPersona, IdRol = idRol, Activo = true });
        }

        await _db.SaveChangesAsync(ct);
    }
}

public sealed record DeletePersonaCommand(int Id) : IRequest<Unit>;

public sealed class DeletePersonaCommandHandler : IRequestHandler<DeletePersonaCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeletePersonaCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeletePersonaCommand command, CancellationToken ct)
    {
        var persona = await _db.Personas.IgnoreQueryFilters()
            .Include(p => p.PersonaRoles).ThenInclude(pr => pr.Rol)
            .FirstOrDefaultAsync(p => p.IdPersona == command.Id, ct)
            ?? throw new NotFoundException($"persona {command.Id}");

        var tieneRolAdministrativo = persona.PersonaRoles
            .Any(pr => pr.Activo && pr.Rol?.Nombre == AppRoles.Administrativo);
        RoleAssignmentPolicy.ValidarBaja(tieneRolAdministrativo);

        persona.Activo = false;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

internal static class RolAdministrativo
{
    public static async Task<int> GetIdAsync(IApplicationDbContext db, CancellationToken ct)
    {
        var rol = await db.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Nombre == AppRoles.Administrativo, ct);

        return rol?.IdRol
            ?? throw new ReglaDeNegocioException("No está configurado el rol 'administrativo'.");
    }
}
