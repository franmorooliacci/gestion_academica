namespace GestionAcademica.Infrastructure.Security;

/// <summary>Configuración de JWT leída desde appsettings ("Jwt").</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "GestionAcademica";
    public string Audience { get; set; } = "GestionAcademica.Clients";
    public string Secret { get; set; } = null!;
    public int ExpirationMinutes { get; set; } = 480;
}
