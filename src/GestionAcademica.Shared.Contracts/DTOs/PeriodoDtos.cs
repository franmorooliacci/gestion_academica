using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record PeriodoDto(
    int Id,
    short Anio,
    TipoPeriodo Tipo,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    bool InscripcionComisionesAbierta,
    bool InscripcionFinalesAbierta,
    bool Activo);

public sealed record CrearPeriodoRequest(
    short Anio,
    TipoPeriodo Tipo,
    DateOnly FechaInicio,
    DateOnly FechaFin);

public sealed record ActualizarPeriodoRequest(
    short Anio,
    TipoPeriodo Tipo,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    bool Activo);

public sealed record ActualizarVentanasRequest(bool InscripcionComisionesAbierta, bool InscripcionFinalesAbierta);
