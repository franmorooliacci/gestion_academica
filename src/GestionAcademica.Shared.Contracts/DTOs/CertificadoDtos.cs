using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Shared.Contracts.DTOs;

public sealed record CertificadoDto(
    int Id,
    int IdInscripcionCarrera,
    int IdPersona,
    string PersonaNombre,
    int IdCarrera,
    string CarreraNombre,
    TipoCertificado Tipo,
    DateTime FechaGeneracion,
    string CodigoValidacion,
    bool TieneArchivo);

public sealed record GenerarCertificadoRequest(int IdInscripcionCarrera, TipoCertificado Tipo);

/// <summary>
/// Resultado de validar un certificado por su código público (endpoint anónimo al que
/// apunta el QR impreso en el PDF).
/// </summary>
public sealed record ValidacionCertificadoDto(
    bool Valido,
    string Codigo,
    string? PersonaNombre,
    string? CarreraNombre,
    TipoCertificado? Tipo,
    DateTime? FechaGeneracion);
