using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.Certificados;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/certificados")]
[Authorize(Policy = Policies.Alumno)]
public sealed class CertificadosController : ApiControllerBase
{
    private readonly ISender _sender;
    public CertificadosController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<List<CertificadoDto>>> GetAll(
        [FromQuery] int? idInscripcionCarrera, CancellationToken ct)
        => Ok(await _sender.Send(
            new GetCertificadosQuery(idInscripcionCarrera, SolicitanteActual), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CertificadoDto>> GetById(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetCertificadoByIdQuery(id, SolicitanteActual), ct));

    /// <summary>Genera el PDF, lo guarda en la carpeta protegida y registra la ruta en la BD.</summary>
    [HttpPost]
    public async Task<ActionResult<CertificadoDto>> Generar(
        [FromBody] GenerarCertificadoRequest request, CancellationToken ct)
    {
        var dto = await _sender.Send(
            new GenerarCertificadoCommand(request, SolicitanteActual), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>Descarga el PDF. El alumno solo puede descargar sus propios certificados.</summary>
    [HttpGet("{id:int}/archivo")]
    public async Task<IActionResult> Descargar(int id, CancellationToken ct)
    {
        var archivo = await _sender.Send(
            new DescargarCertificadoQuery(id, SolicitanteActual), ct);

        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    /// <summary>Destino del QR: validación pública de un certificado por su código.</summary>
    [HttpGet("validar/{codigo}")]
    [AllowAnonymous]
    public async Task<ActionResult<ValidacionCertificadoDto>> Validar(string codigo, CancellationToken ct)
        => Ok(await _sender.Send(new ValidarCertificadoQuery(codigo), ct));
}
