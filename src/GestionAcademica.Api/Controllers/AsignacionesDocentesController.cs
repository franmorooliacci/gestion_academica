using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/asignaciones-docentes")]
public sealed class AsignacionesDocentesController : ApiControllerBase
{
    private readonly ISender _sender;
    public AsignacionesDocentesController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = Policies.AdministrativoOProfesor)]
    public async Task<ActionResult<List<AsignacionDocenteDto>>> GetAll(
        [FromQuery] int? idMateria, [FromQuery] int? idPersona, CancellationToken ct)
        => Ok(await _sender.Send(
            new GetAsignacionesDocentesQuery(idMateria, idPersona, SolicitanteActual), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<AsignacionDocenteDto>> Create(
        [FromBody] CrearAsignacionDocenteRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new CreateAsignacionDocenteCommand(request), ct));

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<AsignacionDocenteDto>> Update(
        int id, [FromBody] ActualizarAsignacionDocenteRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateAsignacionDocenteCommand(id, request), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _sender.Send(new DeleteAsignacionDocenteCommand(id), ct);
        return NoContent();
    }
}
