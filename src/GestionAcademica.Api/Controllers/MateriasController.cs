using GestionAcademica.Api.Authorization;
using GestionAcademica.Application.UseCases.GestionAcademica;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/materias")]
public sealed class MateriasController : ApiControllerBase
{
    private readonly ISender _sender;
    public MateriasController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<MateriaDto>>> GetAll([FromQuery] int? idCarrera, CancellationToken ct)
        => Ok(await _sender.Send(new GetMateriasQuery(idCarrera), ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<MateriaDto>> GetById(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetMateriaByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<MateriaDto>> Create([FromBody] CrearMateriaRequest request, CancellationToken ct)
    {
        var dto = await _sender.Send(new CreateMateriaCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<MateriaDto>> Update(int id, [FromBody] ActualizarMateriaRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateMateriaCommand(id, request), ct));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _sender.Send(new DeleteMateriaCommand(id), ct);
        return NoContent();
    }

    // ---------- Correlatividades ----------

    [HttpGet("{id:int}/correlatividades")]
    [Authorize]
    public async Task<ActionResult<List<CorrelatividadDto>>> GetCorrelatividades(int id, CancellationToken ct)
        => Ok(await _sender.Send(new GetCorrelatividadesQuery(id), ct));

    [HttpPost("{id:int}/correlatividades")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<ActionResult<CorrelatividadDto>> AddCorrelatividad(
        int id, [FromBody] CrearCorrelatividadRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new CreateCorrelatividadCommand(id, request), ct));

    [HttpDelete("{id:int}/correlatividades/{idCorrelatividad:int}")]
    [Authorize(Policy = Policies.Administrativo)]
    public async Task<IActionResult> DeleteCorrelatividad(int id, int idCorrelatividad, CancellationToken ct)
    {
        await _sender.Send(new DeleteCorrelatividadCommand(id, idCorrelatividad), ct);
        return NoContent();
    }
}
