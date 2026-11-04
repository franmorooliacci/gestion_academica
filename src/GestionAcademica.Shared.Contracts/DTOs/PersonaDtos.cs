using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record PersonaDto(
    int Id,
    string Nombre,
    string Apellido,
    string Dni,
    Sexo Sexo,
    DateOnly FechaNacimiento,
    string? Telefono,
    string? Domicilio,
    string Email,
    string Username,
    bool Activo,
    IReadOnlyList<RolDto> Roles,
    IReadOnlyList<CarreraActivaDto> Carreras);

public sealed record CrearPersonaRequest(
    string Nombre,
    string Apellido,
    string Dni,
    Sexo Sexo,
    DateOnly FechaNacimiento,
    string? Telefono,
    string? Domicilio,
    string Email,
    string Username,
    string Password,
    IReadOnlyList<int>? IdsRoles);

public sealed record ActualizarPersonaRequest(
    string Nombre,
    string Apellido,
    string Dni,
    Sexo Sexo,
    DateOnly FechaNacimiento,
    string? Telefono,
    string? Domicilio,
    string Email,
    string Username,
    IReadOnlyList<int>? IdsRoles);
