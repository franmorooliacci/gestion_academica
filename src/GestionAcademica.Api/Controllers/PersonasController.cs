using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/personas")]
[Authorize(Policy = Policies.Administrativo)]
public sealed class PersonasController : ApiControllerBase
{
    private readonly ISender _sender;
    public PersonasController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<PersonaDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
        => Ok(await _sender.Send(new GetPersonasQuery(search), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PersonaDto>> GetById(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetPersonaByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<PersonaDto>> Create([FromBody] CrearPersonaRequest request, CancellationToken ct)
    {
        var dto = await _sender.Send(new CreatePersonaCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PersonaDto>> Update(int id, [FromBody] ActualizarPersonaRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdatePersonaCommand(id, request), ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _sender.Send(new DeletePersonaCommand(id), ct);
        return NoContent();
    }
}
