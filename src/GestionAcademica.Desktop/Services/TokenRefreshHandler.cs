using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace GestionAcademica.Desktop.Services;

/// <summary>
/// Renueva el token antes de que venza y reintenta una vez la request si la API
/// responde 401. Si el refresh ya no es posible, la sesión vuelve al login.
/// Debe ubicarse antes que <see cref="AuthHeaderHandler"/> en la cadena de handlers.
/// </summary>
public sealed class TokenRefreshHandler(
    SessionService session,
    ITokenRefreshService refreshService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        if (session.IsTokenExpiringSoon && !string.IsNullOrWhiteSpace(session.Token))
            await refreshService.TryRefreshAsync(session.Token, ct);

        // El cuerpo se bufferea para poder reintentar la request después del refresh.
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(ct);

        var tokenUsed = session.Token;
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized
            || string.IsNullOrWhiteSpace(tokenUsed))
        {
            return response;
        }

        if (!await refreshService.TryRefreshAsync(tokenUsed, ct))
            return response;

        response.Dispose();
        return await base.SendAsync(CloneRequest(request, body), ct);
    }

    private HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[]? body)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
        };

        if (body is not null && request.Content is not null)
        {
            var content = new ByteArrayContent(body);
            foreach (var header in request.Content.Headers)
            {
                // Content-Length lo calcula ByteArrayContent a partir del cuerpo.
                if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        // El reintento se hace con el token renovado, sin depender del orden de los handlers.
        clone.Headers.Authorization = string.IsNullOrWhiteSpace(session.Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", session.Token);

        return clone;
    }
}
