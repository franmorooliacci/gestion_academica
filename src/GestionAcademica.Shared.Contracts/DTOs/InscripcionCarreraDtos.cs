using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record InscripcionCarreraDto(
    int Id,
    int IdPersona,
    string PersonaNombre,
    int IdCarrera,
    string CarreraNombre,
    DateTime FechaInscripcion,
    short? AnioUltimaReinscripcion,
    DateTime? FechaReinscripcion,
    EstadoInscripcionCarrera Estado);

public sealed record CrearInscripcionCarreraRequest(int IdPersona, int IdCarrera);

public sealed record ActualizarEstadoInscripcionCarreraRequest(EstadoInscripcionCarrera Estado);

public sealed record ReinscribirRequest(short Anio);
