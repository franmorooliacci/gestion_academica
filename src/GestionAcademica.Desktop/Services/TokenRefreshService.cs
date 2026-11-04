using System.Net;
using System.Net.Http;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface ITokenRefreshService
{
    /// <summary>
    /// Renueva la sesión a partir del token con el que se hizo la request.
    /// Devuelve false si la sesión ya no puede renovarse.
    /// </summary>
    Task<bool> TryRefreshAsync(string staleToken, CancellationToken ct);
}

/// <summary>
/// Renueva el JWT usando el endpoint anónimo de refresh. Los intentos concurrentes
/// se serializan para no emitir varios tokens a la vez.
/// </summary>
public sealed class TokenRefreshService(SessionService session, IAuthApi auth) : ITokenRefreshService
{
    public async Task<bool> TryRefreshAsync(string staleToken, CancellationToken ct)
    {
        // Otra request ya renovó la sesión mientras esta esperaba.
        if (session.Token != staleToken)
            return session.IsAuthenticated;

        await session.RefreshGate.WaitAsync(ct);

        try
        {
            if (session.Token != staleToken)
                return session.IsAuthenticated;

            if (string.IsNullOrWhiteSpace(session.Token))
            {
                session.Expire();
                return false;
            }

            try
            {
                var response = await auth.RefreshAsync(
                    new RefreshTokenRequest(session.Token), ct);
                session.Set(response);
                return true;
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                session.Expire();
                return false;
            }
            catch (ApiException)
            {
                // La API no pudo renovar por un error transitorio: no se cierra la sesión.
                return false;
            }
        }
        finally
        {
            session.RefreshGate.Release();
        }
    }
}
