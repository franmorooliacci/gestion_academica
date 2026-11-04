using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

// ---------- Mesas de examen (examenes) ----------

public sealed record ExamenDto(
    int Id,
    int IdMateria,
    string MateriaNombre,
    int? IdPeriodo,
    short? PeriodoAnio,
    TipoPeriodo? PeriodoTipo,
    bool InscripcionFinalesAbierta,
    DateTime Fecha,
    string? Aula);

public sealed record CrearExamenRequest(
    int IdMateria,
    int? IdPeriodo,
    DateTime Fecha,
    string? Aula);

public sealed record ActualizarExamenRequest(
    int IdMateria,
    int? IdPeriodo,
    DateTime Fecha,
    string? Aula);

// ---------- Inscripciones a final (inscripciones_examen) ----------

public sealed record InscripcionExamenDto(
    int Id,
    int IdInscripcionCarrera,
    string PersonaNombre,
    int IdExamen,
    EstadoInscripcionExamen Estado,
    decimal? Nota,
    DateTime FechaInscripcion);

public sealed record CrearInscripcionExamenRequest(int IdInscripcionCarrera, int IdExamen);

/// <summary>Cierre de acta: estado final y nota de cada inscripción al examen.</summary>
public sealed record CerrarActaExamenRequest(IReadOnlyList<ActaInscripcionItem> Items);

public sealed record ActaInscripcionItem(
    int IdInscripcionExamen,
    EstadoInscripcionExamen Estado,
    decimal? Nota);
