using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record ComisionDto(
    int Id,
    int IdMateria,
    string MateriaNombre,
    int IdPeriodo,
    string Codigo,
    Turno Turno,
    ModalidadDictado ModalidadDictado,
    int? CupoMaximo,
    int? ListaEsperaMaxima,
    bool Activo);

public sealed record CrearComisionRequest(
    int IdMateria,
    int IdPeriodo,
    string Codigo,
    Turno Turno,
    ModalidadDictado ModalidadDictado,
    int? CupoMaximo,
    int? ListaEsperaMaxima);

public sealed record ActualizarComisionRequest(
    int IdMateria,
    int IdPeriodo,
    string Codigo,
    Turno Turno,
    ModalidadDictado ModalidadDictado,
    int? CupoMaximo,
    int? ListaEsperaMaxima,
    bool Activo);
