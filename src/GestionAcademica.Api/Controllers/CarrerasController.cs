using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/carreras")]
public sealed class CarrerasController : ApiControllerBase
{
    private readonly ISender _sender;
    public CarrerasController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<CarreraDto>>> GetAll(CancellationToken ct)
        => Ok(await _sender.Send(new GetCarrerasQuery(), ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<CarreraDto>> GetById(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetCarreraByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<CarreraDto>> Create([FromBody] CrearCarreraRequest request, CancellationToken ct)
    {
        var dto = await _sender.Send(new CreateCarreraCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<CarreraDto>> Update(int id, [FromBody] ActualizarCarreraRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateCarreraCommand(id, request), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _sender.Send(new DeleteCarreraCommand(id), ct);
        return NoContent();
    }
}
