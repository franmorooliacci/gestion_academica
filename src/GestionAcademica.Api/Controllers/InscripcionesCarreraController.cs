using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/inscripciones-carrera")]
public sealed class InscripcionesCarreraController : ApiControllerBase
{
    private readonly ISender _sender;
    public InscripcionesCarreraController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = Policies.AdministrativoOAlumno)]
    public async Task<ActionResult<List<InscripcionCarreraDto>>> GetAll(
        [FromQuery] int? idPersona, [FromQuery] int? idCarrera, CancellationToken ct)
        => Ok(await _sender.Send(
            new GetInscripcionesCarreraQuery(idPersona, idCarrera, SolicitanteActual), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<InscripcionCarreraDto>> Create(
        [FromBody] CrearInscripcionCarreraRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new CreateInscripcionCarreraCommand(request), ct));

    [HttpPost("{id:int}/reinscribir")]
    [Authorize(Policy = Policies.Alumno)]
    public async Task<ActionResult<InscripcionCarreraDto>> Reinscribir(
        int id, [FromBody] ReinscribirRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new ReinscribirCommand(id, request, SolicitanteActual), ct));

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<InscripcionCarreraDto>> UpdateEstado(
        int id, [FromBody] ActualizarEstadoInscripcionCarreraRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateEstadoInscripcionCarreraCommand(id, request), ct));
}
