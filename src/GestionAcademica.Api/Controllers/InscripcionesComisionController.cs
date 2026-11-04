using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/inscripciones-comision")]
public sealed class InscripcionesComisionController : ApiControllerBase
{
    private readonly ISender _sender;
    public InscripcionesComisionController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<InscripcionComisionDto>>> GetAll(
        [FromQuery] int? idComision, [FromQuery] int? idInscripcionCarrera, CancellationToken ct)
        => Ok(await _sender.Send(
            new GetInscripcionesComisionQuery(idComision, idInscripcionCarrera, SolicitanteActual), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Alumno)]
    public async Task<ActionResult<InscripcionComisionDto>> Create(
        [FromBody] CrearInscripcionComisionRequest request, CancellationToken ct)
        => Ok(await _sender.Send(
            new CreateInscripcionComisionCommand(request, SolicitanteActual), ct));

    [HttpPost("{id:int}/baja")]
    [Authorize(Policy = Policies.Alumno)]
    public async Task<ActionResult<InscripcionComisionDto>> DarDeBaja(int id, CancellationToken ct)
        => Ok(await _sender.Send(
            new BajaInscripcionComisionCommand(id, SolicitanteActual), ct));

    /// <summary>Carga de condición de cursada: solo el profesor asignado a la comisión.</summary>
    [HttpPatch("{id:int}/condicion")]
    [Authorize(Policy = Policies.Profesor)]
    public async Task<ActionResult<InscripcionComisionDto>> UpdateCondicion(
        int id, [FromBody] ActualizarCondicionRequest request, CancellationToken ct)
        => Ok(await _sender.Send(
            new UpdateCondicionInscripcionComisionCommand(id, request, SolicitanteActual), ct));
}
