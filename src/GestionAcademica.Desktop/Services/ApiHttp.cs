using System.Net.Http;
using System.Net.Http.Json;

namespace GestionAcademica.Desktop.Services;

/// <summary>Utilidades compartidas por los clientes tipados de la API.</summary>
internal static class ApiHttp
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponseAsync(response, ct);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<T>(ApiJson.Options, ct)
            ?? throw new ApiException("La API devolvió una respuesta vacía.", response.StatusCode);
    }

    public static string Query(params (string Name, object? Value)[] parameters)
    {
        var pairs = parameters
            .Where(parameter => parameter.Value is not null)
            .Select(parameter => $"{parameter.Name}={Uri.EscapeDataString(parameter.Value!.ToString()!)}")
            .ToArray();

        return pairs.Length == 0 ? string.Empty : $"?{string.Join("&", pairs)}";
    }
}
