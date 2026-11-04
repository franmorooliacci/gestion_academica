using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record LoginRequest(string Username, string Password, ClientApp? Client);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);

public sealed record SwitchRoleRequest(string Role);

public sealed record UserDto(
    int Id,
    string Nombre,
    string Apellido,
    string Email,
    string Username,
    string? ActiveRole,
    IReadOnlyList<RolDto> Roles,
    IReadOnlyList<CarreraActivaDto> Carreras);

public sealed record RolDto(int Id, string Nombre);

public sealed record CarreraActivaDto(
    int IdInscripcionCarrera,
    int IdCarrera,
    string NombreCarrera,
    EstadoInscripcionCarrera Estado);
