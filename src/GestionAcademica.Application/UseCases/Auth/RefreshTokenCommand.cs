using GestionAcademica.Application.Interfaces;
using GestionAcademica.Application.Services;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using MediatR;

namespace GestionAcademica.Application.UseCases.Auth;

/// <summary>
/// Renueva el JWT de una sesión existente conservando el cliente y el rol activo.
/// El perfil se reconstruye desde la base: si el usuario fue deshabilitado o perdió
/// sus roles, la renovación se rechaza.
/// </summary>
public sealed record RefreshTokenCommand(int PersonaId, string? ActiveRole, ClientApp Client)
    : IRequest<LoginResponse>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IUserProfileService _profileService;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public RefreshTokenCommandHandler(
        IUserProfileService profileService,
        IJwtTokenGenerator tokenGenerator)
    {
        _profileService = profileService;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _profileService.BuildAsync(
            request.PersonaId, request.ActiveRole, request.Client, cancellationToken);

        if (user.Roles.Count == 0)
            throw new AccesoDenegadoException("El usuario no tiene un rol habilitado para este cliente.");

        var token = _tokenGenerator.GenerateToken(
            request.PersonaId,
            user.Username,
            user.Roles.Select(role => role.Nombre).ToList(),
            user.ActiveRole,
            request.Client,
            user.Carreras);

        return new LoginResponse(token.Token, token.ExpiresAt, user);
    }
}
