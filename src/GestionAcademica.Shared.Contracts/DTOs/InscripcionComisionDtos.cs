using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record InscripcionComisionDto(
    int Id,
    int IdInscripcionCarrera,
    int IdComision,
    string ComisionCodigo,
    string MateriaNombre,
    string PersonaNombre,
    EstadoInscripcionComision Estado,
    CondicionCursada? Condicion,
    DateTime FechaInscripcion);

public sealed record CrearInscripcionComisionRequest(int IdInscripcionCarrera, int IdComision);

/// <summary>Carga de condición de cursada por parte del profesor.</summary>
public sealed record ActualizarCondicionRequest(CondicionCursada Condicion);
