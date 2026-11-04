using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Application.Models;

/// <summary>
/// Modelo con todos los datos que se imprimen en un certificado. Se arma en la capa
/// Application a partir de las entidades y lo consume el generador de PDF.
/// </summary>
public sealed record CertificadoPdfModel(
    TipoCertificado Tipo,
    string CodigoValidacion,
    string UrlValidacion,
    string NombreCompleto,
    string Dni,
    string? Email,
    string CarreraNombre,
    TipoCarrera CarreraTipo,
    string TituloOtorgado,
    short DuracionAnios,
    DateTime FechaInscripcion,
    EstadoInscripcionCarrera EstadoInscripcion,
    short? AnioUltimaReinscripcion,
    DateTime FechaGeneracion,
    IReadOnlyList<CertificadoMateriaPdf> Materias);

public sealed record CertificadoMateriaPdf(
    string Codigo,
    string Nombre,
    short AnioEnCarrera,
    EstadoMateriaCertificado Estado,
    decimal? Nota,
    DateTime? FechaAprobacion);

public enum EstadoMateriaCertificado
{
    Aprobada,
    Regular,
    EnCurso,
    Pendiente,
}
