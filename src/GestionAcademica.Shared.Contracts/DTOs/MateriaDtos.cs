using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record MateriaDto(
    int Id,
    int IdCarrera,
    string Codigo,
    string Nombre,
    short AnioEnCarrera,
    RegimenCursada RegimenCursada,
    int CargaHorariaTotal,
    bool Activa);

public sealed record CrearMateriaRequest(
    int IdCarrera,
    string Codigo,
    string Nombre,
    short AnioEnCarrera,
    RegimenCursada RegimenCursada,
    int CargaHorariaTotal);

public sealed record ActualizarMateriaRequest(
    int IdCarrera,
    string Codigo,
    string Nombre,
    short AnioEnCarrera,
    RegimenCursada RegimenCursada,
    int CargaHorariaTotal,
    bool Activa);
