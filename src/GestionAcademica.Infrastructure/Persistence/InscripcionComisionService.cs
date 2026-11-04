using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Domain.Services;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Infrastructure.Persistence;

/// <summary>
/// Implementación transaccional de la inscripción a comisiones. Usa bloqueo pesimista
/// (SELECT ... FOR UPDATE) sobre la fila de la comisión para serializar las altas/bajas
/// concurrentes y evitar la sobreventa de cupos.
/// </summary>
public sealed class InscripcionComisionService : IInscripcionComisionService
{
    private readonly GestionAcademicaDbContext _db;

    public InscripcionComisionService(GestionAcademicaDbContext db) => _db = db;

    public async Task<InscripcionComision> InscribirAsync(int idInscripcionCarrera, int idComision, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var comision = await BloquearComisionAsync(idComision, ct);

        // Evita violar la unicidad (id_inscripcion_carrera, id_comision) en alta concurrente.
        var existente = await _db.InscripcionesComision.FirstOrDefaultAsync(
            i => i.IdInscripcionCarrera == idInscripcionCarrera && i.IdComision == idComision, ct);

        if (existente is not null)
            throw new ReglaDeNegocioException("El alumno ya posee una inscripción en esta comisión.");

        var inscriptos = await ContarPorEstadoAsync(idComision, EstadoInscripcionComision.Inscripto, ct);
        var enEspera = await ContarPorEstadoAsync(idComision, EstadoInscripcionComision.EnEspera, ct);

        var estado = CupoService.DeterminarEstado(
            comision.CupoMaximo, comision.ListaEsperaMaxima, inscriptos, enEspera, comision.Codigo);

        var inscripcion = new InscripcionComision
        {
            IdInscripcionCarrera = idInscripcionCarrera,
            IdComision = idComision,
            Estado = estado,
            Condicion = null,
            FechaInscripcion = DateTime.UtcNow,
        };

        _db.InscripcionesComision.Add(inscripcion);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return inscripcion;
    }

    public async Task DarDeBajaAsync(int idInscripcionComision, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var inscripcion = await _db.InscripcionesComision
            .FirstOrDefaultAsync(i => i.IdInscripcionComision == idInscripcionComision, ct)
            ?? throw new NotFoundException($"inscripción a comisión {idInscripcionComision}");

        if (inscripcion.Estado == EstadoInscripcionComision.Baja)
            throw new ReglaDeNegocioException("La inscripción ya está dada de baja.");

        // Bloquea la comisión para serializar la baja + promoción del siguiente en espera.
        await BloquearComisionAsync(inscripcion.IdComision, ct);

        var liberaCupo = inscripcion.Estado == EstadoInscripcionComision.Inscripto;
        inscripcion.Estado = EstadoInscripcionComision.Baja;

        // Promueve al primer 'en_espera' (por fecha de inscripción) dentro de la misma transacción.
        if (liberaCupo)
        {
            var siguiente = await _db.InscripcionesComision
                .Where(i => i.IdComision == inscripcion.IdComision
                            && i.Estado == EstadoInscripcionComision.EnEspera)
                .OrderBy(i => i.FechaInscripcion)
                .ThenBy(i => i.IdInscripcionComision)
                .FirstOrDefaultAsync(ct);

            if (siguiente is not null)
                siguiente.Estado = EstadoInscripcionComision.Inscripto;
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<Comision> BloquearComisionAsync(int idComision, CancellationToken ct)
    {
        return await _db.Comisiones
            .FromSql($"SELECT * FROM comisiones WHERE id_comision = {idComision} FOR UPDATE")
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"comisión {idComision}");
    }

    private Task<int> ContarPorEstadoAsync(int idComision, EstadoInscripcionComision estado, CancellationToken ct)
    {
        return _db.InscripcionesComision.CountAsync(
            i => i.IdComision == idComision && i.Estado == estado, ct);
    }
}
