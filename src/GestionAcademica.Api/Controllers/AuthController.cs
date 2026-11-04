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

    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _sender.Send(
            new LoginCommand(request.Username, request.Password, request.Client), ct));

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
