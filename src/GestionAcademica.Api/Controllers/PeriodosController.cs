using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/periodos")]
public sealed class PeriodosController : ApiControllerBase
{
    private readonly ISender _sender;
    public PeriodosController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<PeriodoDto>>> GetAll(CancellationToken ct)
        => Ok(await _sender.Send(new GetPeriodosQuery(), ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<PeriodoDto>> GetById(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetPeriodoByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<PeriodoDto>> Create([FromBody] CrearPeriodoRequest request, CancellationToken ct)
    {
        var dto = await _sender.Send(new CreatePeriodoCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<PeriodoDto>> Update(int id, [FromBody] ActualizarPeriodoRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdatePeriodoCommand(id, request), ct));

    [HttpPatch("{id:int}/ventanas")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<PeriodoDto>> UpdateVentanas(int id, [FromBody] ActualizarVentanasRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateVentanasCommand(id, request), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _sender.Send(new DeletePeriodoCommand(id), ct);
        return NoContent();
    }
}
