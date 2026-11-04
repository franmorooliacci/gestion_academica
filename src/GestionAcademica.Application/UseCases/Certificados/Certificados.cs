using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Application.Models;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using GestionAcademica.Shared.Contracts.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.Certificados;

// ---------- Queries ----------

public sealed record GetCertificadosQuery(int? IdInscripcionCarrera, Solicitante Solicitante)
    : IRequest<List<CertificadoDto>>;

public sealed class GetCertificadosQueryHandler
    : IRequestHandler<GetCertificadosQuery, List<CertificadoDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCertificadosQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CertificadoDto>> Handle(GetCertificadosQuery request, CancellationToken ct)
    {
        var query = _db.Certificados.AsNoTracking()
            .Include(c => c.InscripcionCarrera).ThenInclude(ic => ic.Persona)
            .Include(c => c.InscripcionCarrera).ThenInclude(ic => ic.Carrera)
            .Where(c => c.InscripcionCarrera.IdPersona == request.Solicitante.IdPersona)
            .AsQueryable();

        if (request.IdInscripcionCarrera.HasValue)
            query = query.Where(c => c.IdInscripcionCarrera == request.IdInscripcionCarrera.Value);

        var items = await query
            .OrderByDescending(c => c.FechaGeneracion)
            .ThenByDescending(c => c.IdCertificado)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    internal static CertificadoDto ToDto(Certificado c)
    {
        var ic = c.InscripcionCarrera;
        return new CertificadoDto(
            c.IdCertificado,
            c.IdInscripcionCarrera,
            ic?.IdPersona ?? 0,
            ic?.Persona is null ? string.Empty : $"{ic.Persona.Apellido}, {ic.Persona.Nombre}",
            ic?.IdCarrera ?? 0,
            ic?.Carrera?.Nombre ?? string.Empty,
            c.Tipo,
            c.FechaGeneracion,
            c.CodigoValidacion,
            !string.IsNullOrWhiteSpace(c.RutaArchivo));
    }
}

public sealed record GetCertificadoByIdQuery(int Id, Solicitante Solicitante) : IRequest<CertificadoDto>;

public sealed class GetCertificadoByIdQueryHandler : IRequestHandler<GetCertificadoByIdQuery, CertificadoDto>
{
    private readonly IApplicationDbContext _db;
    public GetCertificadoByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CertificadoDto> Handle(GetCertificadoByIdQuery request, CancellationToken ct)
    {
        var certificado = await _db.Certificados.AsNoTracking()
            .Include(c => c.InscripcionCarrera).ThenInclude(ic => ic.Persona)
            .Include(c => c.InscripcionCarrera).ThenInclude(ic => ic.Carrera)
            .FirstOrDefaultAsync(c => c.IdCertificado == request.Id, ct)
            ?? throw new NotFoundException($"certificado {request.Id}");

        if (certificado.InscripcionCarrera.IdPersona != request.Solicitante.IdPersona)
            throw new AccesoDenegadoException("No puede consultar certificados de otra persona.");

        return GetCertificadosQueryHandler.ToDto(certificado);
    }
}

/// <summary>Validación pública (destino del QR): no expone datos sensibles adicionales.</summary>
public sealed record ValidarCertificadoQuery(string Codigo) : IRequest<ValidacionCertificadoDto>;

public sealed class ValidarCertificadoQueryHandler
    : IRequestHandler<ValidarCertificadoQuery, ValidacionCertificadoDto>
{
    private readonly IApplicationDbContext _db;
    public ValidarCertificadoQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ValidacionCertificadoDto> Handle(ValidarCertificadoQuery request, CancellationToken ct)
    {
        var codigo = (request.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        if (codigo.Length != 32)
            return new ValidacionCertificadoDto(false, codigo, null, null, null, null);

        var certificado = await _db.Certificados.AsNoTracking()
            .Include(c => c.InscripcionCarrera).ThenInclude(ic => ic.Persona)
            .Include(c => c.InscripcionCarrera).ThenInclude(ic => ic.Carrera)
            .FirstOrDefaultAsync(c => c.CodigoValidacion == codigo, ct);

        if (certificado is null)
            return new ValidacionCertificadoDto(false, codigo, null, null, null, null);

        var ic = certificado.InscripcionCarrera;
        return new ValidacionCertificadoDto(
            true,
            codigo,
            ic.Persona is null ? null : $"{ic.Persona.Apellido}, {ic.Persona.Nombre}",
            ic.Carrera?.Nombre,
            certificado.Tipo,
            certificado.FechaGeneracion);
    }
}

public sealed record DescargarCertificadoQuery(int Id, Solicitante Solicitante)
    : IRequest<CertificadoArchivo>;

public sealed class DescargarCertificadoQueryHandler
    : IRequestHandler<DescargarCertificadoQuery, CertificadoArchivo>
{
    private readonly IApplicationDbContext _db;
    private readonly ICertificadoFileStorage _storage;

    public DescargarCertificadoQueryHandler(IApplicationDbContext db, ICertificadoFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<CertificadoArchivo> Handle(DescargarCertificadoQuery request, CancellationToken ct)
    {
        var certificado = await _db.Certificados.AsNoTracking()
            .Include(c => c.InscripcionCarrera)
            .FirstOrDefaultAsync(c => c.IdCertificado == request.Id, ct)
            ?? throw new NotFoundException($"certificado {request.Id}");

        // El alumno solo puede descargar sus propios certificados.
        if (certificado.InscripcionCarrera.IdPersona != request.Solicitante.IdPersona)
            throw new AccesoDenegadoException("No puede descargar certificados de otra persona.");

        if (string.IsNullOrWhiteSpace(certificado.RutaArchivo))
            throw new ReglaDeNegocioException("El certificado no tiene un archivo asociado.");

        var contenido = await _storage.LeerAsync(certificado.RutaArchivo, ct)
            ?? throw new NotFoundException($"archivo del certificado {certificado.IdCertificado}");

        var nombre = $"certificado-{certificado.Tipo.ToValue()}-{certificado.CodigoValidacion}.pdf";
        return new CertificadoArchivo(contenido, nombre, "application/pdf");
    }
}

// ---------- Commands ----------

public sealed record GenerarCertificadoCommand(GenerarCertificadoRequest Request, Solicitante Solicitante)
    : IRequest<CertificadoDto>;

public sealed class GenerarCertificadoCommandHandler
    : IRequestHandler<GenerarCertificadoCommand, CertificadoDto>
{
    /// <summary>Nota mínima de final aprobatoria (misma regla que el cierre de actas).</summary>
    private const decimal NotaAprobatoria = 4m;

    private readonly IApplicationDbContext _db;
    private readonly ICertificadoPdfGenerator _pdf;
    private readonly ICertificadoFileStorage _storage;
    private readonly ICertificadoSettings _settings;

    public GenerarCertificadoCommandHandler(
        IApplicationDbContext db,
        ICertificadoPdfGenerator pdf,
        ICertificadoFileStorage storage,
        ICertificadoSettings settings)
    {
        _db = db;
        _pdf = pdf;
        _storage = storage;
        _settings = settings;
    }

    public async Task<CertificadoDto> Handle(GenerarCertificadoCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var inscripcion = await _db.InscripcionesCarrera.AsNoTracking()
            .Include(ic => ic.Persona)
            .Include(ic => ic.Carrera)
            .FirstOrDefaultAsync(ic => ic.IdInscripcionCarrera == req.IdInscripcionCarrera, ct)
            ?? throw new NotFoundException($"inscripción a carrera {req.IdInscripcionCarrera}");

        if (inscripcion.IdPersona != command.Solicitante.IdPersona)
            throw new AccesoDenegadoException("No puede generar certificados de otra persona.");

        var materias = req.Tipo switch
        {
            TipoCertificado.Analitico => await MateriasAnaliticoAsync(inscripcion, ct),
            TipoCertificado.AlumnoRegular => await MateriasAlumnoRegularAsync(inscripcion, ct),
            TipoCertificado.ConstanciaInscripcion => Array.Empty<CertificadoMateriaPdf>(),
            _ => throw new ReglaDeNegocioException("Tipo de certificado inválido."),
        };

        var ahora = DateTime.Now;
        var codigo = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var modelo = new CertificadoPdfModel(
            req.Tipo,
            codigo,
            $"{_settings.UrlPublicaBase.TrimEnd('/')}/api/certificados/validar/{codigo}",
            $"{inscripcion.Persona.Apellido}, {inscripcion.Persona.Nombre}",
            inscripcion.Persona.Dni,
            inscripcion.Persona.Email,
            inscripcion.Carrera.Nombre,
            inscripcion.Carrera.Tipo,
            inscripcion.Carrera.TituloOtorgado,
            inscripcion.Carrera.DuracionAnios,
            inscripcion.FechaInscripcion,
            inscripcion.Estado,
            inscripcion.AnioUltimaReinscripcion,
            ahora,
            materias);

        var contenido = _pdf.Generar(modelo);

        var certificado = new Certificado
        {
            IdInscripcionCarrera = inscripcion.IdInscripcionCarrera,
            Tipo = req.Tipo,
            FechaGeneracion = ahora,
            CodigoValidacion = codigo,
        };

        _db.Certificados.Add(certificado);
        await _db.SaveChangesAsync(ct);

        var nombreArchivo = $"{certificado.IdCertificado}-{codigo}.pdf";
        try
        {
            await _storage.GuardarAsync(nombreArchivo, contenido, ct);
            certificado.RutaArchivo = nombreArchivo;
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Compensación: ni fila huérfana ni archivo huérfano.
            await _storage.EliminarAsync(nombreArchivo, CancellationToken.None);
            _db.Certificados.Remove(certificado);
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        certificado.InscripcionCarrera = inscripcion;
        return GetCertificadosQueryHandler.ToDto(certificado);
    }

    /// <summary>
    /// Historial académico completo: aprobadas (final ≥ 4), regularizadas, en curso o pendientes.
    /// </summary>
    private async Task<IReadOnlyList<CertificadoMateriaPdf>> MateriasAnaliticoAsync(
        InscripcionCarrera inscripcion, CancellationToken ct)
    {
        var materias = await _db.Materias.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.IdCarrera == inscripcion.IdCarrera)
            .OrderBy(m => m.AnioEnCarrera).ThenBy(m => m.Nombre)
            .ToListAsync(ct);

        var finales = await _db.InscripcionesExamen.AsNoTracking()
            .Include(i => i.Examen)
            .Where(i => i.IdInscripcionCarrera == inscripcion.IdInscripcionCarrera
                        && i.Nota != null && i.Nota >= NotaAprobatoria)
            .ToListAsync(ct);

        var cursadas = await _db.InscripcionesComision.AsNoTracking()
            .Include(i => i.Comision)
            .Where(i => i.IdInscripcionCarrera == inscripcion.IdInscripcionCarrera
                        && i.Estado != EstadoInscripcionComision.Baja)
            .ToListAsync(ct);

        // Si hubiera más de un final aprobado, se informa la mejor nota.
        var aprobadas = finales
            .GroupBy(f => f.Examen.IdMateria)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(f => f.Nota).ThenByDescending(f => f.Examen.Fecha).First());

        var regularizadas = cursadas
            .Where(c => c.Condicion == CondicionCursada.Regular)
            .Select(c => c.Comision.IdMateria)
            .ToHashSet();

        var enCurso = cursadas
            .Where(c => c.Estado == EstadoInscripcionComision.Inscripto)
            .Select(c => c.Comision.IdMateria)
            .ToHashSet();

        return materias.Select(m =>
        {
            if (aprobadas.TryGetValue(m.IdMateria, out var final))
                return new CertificadoMateriaPdf(
                    m.Codigo, m.Nombre, m.AnioEnCarrera,
                    EstadoMateriaCertificado.Aprobada, final.Nota, final.Examen.Fecha);

            if (regularizadas.Contains(m.IdMateria))
                return new CertificadoMateriaPdf(
                    m.Codigo, m.Nombre, m.AnioEnCarrera,
                    EstadoMateriaCertificado.Regular, null, null);

            if (enCurso.Contains(m.IdMateria))
                return new CertificadoMateriaPdf(
                    m.Codigo, m.Nombre, m.AnioEnCarrera,
                    EstadoMateriaCertificado.EnCurso, null, null);

            return new CertificadoMateriaPdf(
                m.Codigo, m.Nombre, m.AnioEnCarrera,
                EstadoMateriaCertificado.Pendiente, null, null);
        }).ToList();
    }

    /// <summary>
    /// Materias en curso del último período con inscripciones activas. Si no hay ninguna,
    /// el alumno no reviste la condición de regular y no corresponde emitir el certificado.
    /// </summary>
    private async Task<IReadOnlyList<CertificadoMateriaPdf>> MateriasAlumnoRegularAsync(
        InscripcionCarrera inscripcion, CancellationToken ct)
    {
        var cursadas = await _db.InscripcionesComision.AsNoTracking()
            .Include(i => i.Comision).ThenInclude(c => c.Materia)
            .Include(i => i.Comision).ThenInclude(c => c.Periodo)
            .Where(i => i.IdInscripcionCarrera == inscripcion.IdInscripcionCarrera
                        && i.Estado == EstadoInscripcionComision.Inscripto
                        && i.Comision.Materia.IdCarrera == inscripcion.IdCarrera)
            .ToListAsync(ct);

        if (cursadas.Count == 0)
            throw new ReglaDeNegocioException(
                "El alumno no registra materias en curso; no corresponde emitir un certificado de alumno regular.");

        var ultimoPeriodo = cursadas
            .MaxBy(c => c.Comision.Periodo.FechaInicio)!
            .Comision.IdPeriodo;

        return cursadas
            .Where(c => c.Comision.IdPeriodo == ultimoPeriodo)
            .OrderBy(c => c.Comision.Materia.AnioEnCarrera)
            .ThenBy(c => c.Comision.Materia.Nombre)
            .Select(c => new CertificadoMateriaPdf(
                c.Comision.Materia.Codigo,
                c.Comision.Materia.Nombre,
                c.Comision.Materia.AnioEnCarrera,
                c.Condicion == CondicionCursada.Regular
                    ? EstadoMateriaCertificado.Regular
                    : EstadoMateriaCertificado.EnCurso,
                null,
                null))
            .ToList();
    }
}
