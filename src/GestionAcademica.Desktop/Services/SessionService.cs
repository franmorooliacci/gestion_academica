using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

/// <summary>
/// Sesión en memoria: el JWT nunca se persiste en disco. Conserva además el contexto
/// de trabajo (carrera activa) y avisa cuando la sesión debe volver al login.
/// </summary>
public sealed class SessionService
{
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(2);

    /// <summary>Serializa los intentos de refresh concurrentes de la sesión.</summary>
    internal SemaphoreSlim RefreshGate { get; } = new(1, 1);

    public string? Token { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public UserDto? User { get; private set; }

    public CarreraDto? CarreraActiva { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    /// <summary>El token está por vencer (o no se conoce su vencimiento).</summary>
    public bool IsTokenExpiringSoon =>
        IsAuthenticated
        && (ExpiresAt is null || AsUtc(ExpiresAt.Value) - DateTime.UtcNow <= RefreshWindow);

    /// <summary>La sesión dejó de ser válida y el shell debe volver al login.</summary>
    public event EventHandler? SessionExpired;

    /// <summary>Cambió la carrera activa del contexto de trabajo.</summary>
    public event EventHandler? CarreraActivaChanged;

    public void Set(LoginResponse response)
    {
        Token = response.Token;
        ExpiresAt = response.ExpiresAt;
        User = response.User;
    }

    public void SetCarreraActiva(CarreraDto? carrera)
    {
        if (CarreraActiva?.Id == carrera?.Id)
            return;

        CarreraActiva = carrera;
        CarreraActivaChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Invalida la sesión y notifica al shell para que vuelva al login.</summary>
    public void Expire()
    {
        var hadSession = IsAuthenticated || User is not null;

        Clear();

        if (hadSession)
            SessionExpired?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Token = null;
        ExpiresAt = null;
        User = null;
        CarreraActiva = null;
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
