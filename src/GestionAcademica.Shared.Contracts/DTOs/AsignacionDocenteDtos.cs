using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record AsignacionDocenteDto(
    int Id,
    int IdMateria,
    string MateriaNombre,
    int IdPersona,
    string DocenteNombre,
    int? IdComision,
    string? ComisionCodigo,
    CargoDocente Cargo,
    DateOnly FechaDesde,
    DateOnly? FechaHasta,
    bool Activo);

public sealed record CrearAsignacionDocenteRequest(
    int IdMateria,
    int IdPersona,
    int? IdComision,
    CargoDocente Cargo,
    DateOnly FechaDesde,
    DateOnly? FechaHasta);

public sealed record ActualizarAsignacionDocenteRequest(
    int IdMateria,
    int IdPersona,
    int? IdComision,
    CargoDocente Cargo,
    DateOnly FechaDesde,
    DateOnly? FechaHasta,
    bool Activo);
