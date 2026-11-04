using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = Policies.Administrativo)]
public sealed class RolesController : ApiControllerBase
{
    private readonly ISender _sender;
    public RolesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<RolDto>>> GetAll(CancellationToken ct)
        => Ok(await _sender.Send(new GetRolesQuery(), ct));
}
