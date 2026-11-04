using System.Net.Http;
using System.Net.Http.Headers;

namespace GestionAcademica.Desktop.Services;

/// <summary>Agrega el Bearer token de la sesión a cada request autenticado.</summary>
public sealed class AuthHeaderHandler(SessionService session) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(session.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        return base.SendAsync(request, ct);
    }
}
