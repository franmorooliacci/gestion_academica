using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record CarreraDto(
    int Id,
    string Nombre,
    TipoCarrera Tipo,
    string TituloOtorgado,
    short DuracionAnios,
    bool Activa);

public sealed record CrearCarreraRequest(
    string Nombre,
    TipoCarrera Tipo,
    string TituloOtorgado,
    short DuracionAnios);

public sealed record ActualizarCarreraRequest(
    string Nombre,
    TipoCarrera Tipo,
    string TituloOtorgado,
    short DuracionAnios,
    bool Activa);
