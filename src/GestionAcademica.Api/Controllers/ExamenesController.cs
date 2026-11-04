using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/examenes")]
public sealed class ExamenesController : ApiControllerBase
{
    private readonly ISender _sender;
    public ExamenesController(ISender sender) => _sender = sender;

    // ---------- Mesas de examen ----------

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ExamenDto>>> GetAll(
        [FromQuery] int? idMateria, [FromQuery] int? idPeriodo, CancellationToken ct)
        => Ok(await _sender.Send(new GetExamenesQuery(idMateria, idPeriodo), ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ExamenDto>> GetById(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetExamenByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<ExamenDto>> Create([FromBody] CrearExamenRequest request, CancellationToken ct)
    {
        var dto = await _sender.Send(new CreateExamenCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<ExamenDto>> Update(int id, [FromBody] ActualizarExamenRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateExamenCommand(id, request), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _sender.Send(new DeleteExamenCommand(id), ct);
        return NoContent();
    }

    // ---------- Inscripciones a final ----------

    [HttpGet("{id:int}/inscripciones")]
    [Authorize]
    public async Task<ActionResult<List<InscripcionExamenDto>>> GetInscripciones(int id, CancellationToken ct)
        => Ok(await _sender.Send(
            new GetInscripcionesExamenQuery(id, null, SolicitanteActual), ct));

    [HttpPost("{id:int}/inscripciones")]
    [Authorize(Policy = Policies.Alumno)]
    public async Task<ActionResult<InscripcionExamenDto>> Inscribir(
        int id, [FromBody] CrearInscripcionExamenRequest request, CancellationToken ct)
        => Ok(await _sender.Send(
            new CreateInscripcionExamenCommand(request with { IdExamen = id }, SolicitanteActual), ct));

    // ---------- Cierre de acta ----------

    [HttpPatch("{id:int}/acta")]
    [Authorize(Policy = Policies.Profesor)]
    public async Task<ActionResult<List<InscripcionExamenDto>>> CerrarActa(
        int id, [FromBody] CerrarActaExamenRequest request, CancellationToken ct)
        => Ok(await _sender.Send(
            new CerrarActaExamenCommand(id, request, SolicitanteActual), ct));
}
