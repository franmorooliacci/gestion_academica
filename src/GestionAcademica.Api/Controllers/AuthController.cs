using GestionAcademica.Application.Interfaces;
using GestionAcademica.Application.UseCases.Auth;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AuthController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly IJwtTokenParser _tokenParser;

    public AuthController(ISender sender, IJwtTokenParser tokenParser)
    {
        _sender = sender;
        _tokenParser = tokenParser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _sender.Send(
            new LoginCommand(request.Username, request.Password, request.Client), ct));

    /// <summary>
    /// Renueva un JWT vencido (o por vencer). Acepta el token en el cuerpo y valida
    /// firma, emisor y audiencia; no exige que siga vigente. El cliente y el rol activo
    /// se conservan, y el perfil se revalida contra la base.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var session = _tokenParser.Parse(request.Token);
        if (session is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sesión inválida",
                Detail = "La sesión no es válida o expiró. Iniciá sesión nuevamente.",
            });
        }

        return Ok(await _sender.Send(
            new RefreshTokenCommand(session.PersonaId, session.ActiveRole, session.Client), ct));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var solicitante = SolicitanteActual;
        return Ok(await _sender.Send(
            new GetMeQuery(solicitante.IdPersona, solicitante.RolActivo, Cliente), ct));
    }

    [HttpPost("switch-role")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> SwitchRole([FromBody] SwitchRoleRequest request, CancellationToken ct)
        => Ok(await _sender.Send(
            new SwitchRoleCommand(SolicitanteActual.IdPersona, request.Role, Cliente), ct));
}
