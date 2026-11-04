using GestionAcademica.Application.Interfaces;
using GestionAcademica.Application.Services;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.Auth;

public sealed record LoginCommand(string Username, string Password, ClientApp? Client) : IRequest<LoginResponse>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IUserProfileService _profileService;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IUserProfileService profileService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _profileService = profileService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (request.Client is null)
            throw new ReglaDeNegocioException("El cliente es obligatorio (desktop o web).");

        var persona = await _db.Personas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Username == request.Username && p.Activo, cancellationToken);

        if (persona is null || !_passwordHasher.Verify(request.Password, persona.PasswordHash))
            throw new CredencialesInvalidasException();

        var user = await _profileService.BuildAsync(
            persona.IdPersona, null, request.Client.Value, cancellationToken);

        if (user.Roles.Count == 0)
            throw new AccesoDenegadoException("El usuario no tiene un rol habilitado para este cliente.");

        var token = _tokenGenerator.GenerateToken(
            persona.IdPersona,
            persona.Username,
            user.Roles.Select(r => r.Nombre).ToList(),
            user.ActiveRole,
            request.Client.Value,
            user.Carreras);

        return new LoginResponse(token.Token, token.ExpiresAt, user);
    }
}

public sealed record GetMeQuery(int PersonaId, string? ActiveRole, ClientApp Client) : IRequest<UserDto>;

public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto>
{
    private readonly IUserProfileService _profileService;

    public GetMeQueryHandler(IUserProfileService profileService) => _profileService = profileService;

    public Task<UserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
        => _profileService.BuildAsync(request.PersonaId, request.ActiveRole, request.Client, cancellationToken);
}

public sealed record SwitchRoleCommand(int PersonaId, string Role, ClientApp Client) : IRequest<LoginResponse>;

public sealed class SwitchRoleCommandHandler : IRequestHandler<SwitchRoleCommand, LoginResponse>
{
    private readonly IUserProfileService _profileService;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public SwitchRoleCommandHandler(IUserProfileService profileService, IJwtTokenGenerator tokenGenerator)
    {
        _profileService = profileService;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponse> Handle(SwitchRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _profileService.BuildAsync(
            request.PersonaId, null, request.Client, cancellationToken);

        var role = user.Roles.FirstOrDefault(r =>
            string.Equals(r.Nombre, request.Role, StringComparison.OrdinalIgnoreCase));

        if (role is null)
            throw new AccesoDenegadoException(
                $"El rol '{request.Role}' no está habilitado para este cliente.");

        var token = _tokenGenerator.GenerateToken(
            request.PersonaId,
            user.Username,
            user.Roles.Select(r => r.Nombre).ToList(),
            role.Nombre,
            request.Client,
            user.Carreras);

        var refreshed = await _profileService.BuildAsync(
            request.PersonaId, role.Nombre, request.Client, cancellationToken);

        return new LoginResponse(token.Token, token.ExpiresAt, refreshed);
    }
}
