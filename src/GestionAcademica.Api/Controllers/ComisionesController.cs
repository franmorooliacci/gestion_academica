using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/comisiones")]
public sealed class ComisionesController : ApiControllerBase
{
    private readonly ISender _sender;
    public ComisionesController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ComisionDto>>> GetAll(
        [FromQuery] int? idMateria, [FromQuery] int? idPeriodo, CancellationToken ct)
        => Ok(await _sender.Send(new GetComisionesQuery(idMateria, idPeriodo), ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ComisionDto>> GetById(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetComisionByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<ComisionDto>> Create([FromBody] CrearComisionRequest request, CancellationToken ct)
    {
        var dto = await _sender.Send(new CreateComisionCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<ComisionDto>> Update(int id, [FromBody] ActualizarComisionRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateComisionCommand(id, request), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _sender.Send(new DeleteComisionCommand(id), ct);
        return NoContent();
    }
}
