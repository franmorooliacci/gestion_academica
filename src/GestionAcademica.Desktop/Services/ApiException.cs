using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace GestionAcademica.Desktop.Services;

/// <summary>
/// Error devuelto por la API (ProblemDetails). El mensaje ya viene listo para mostrar al usuario.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(
        string message,
        HttpStatusCode statusCode,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public HttpStatusCode StatusCode { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        var message = $"La API respondió {(int)response.StatusCode} ({response.ReasonPhrase}).";
        Dictionary<string, string[]>? errors = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                    {
                        errors = errorsElement.EnumerateObject()
                            .ToDictionary(
                                property => property.Name,
                                property => property.Value.ValueKind == JsonValueKind.Array
                                    ? property.Value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray()
                                    : [property.Value.GetString() ?? string.Empty]);
                    }

                    var detail = ReadString(root, "detail");
                    var title = ReadString(root, "title");

                    if (errors is { Count: > 0 })
                        message = string.Join(" ", errors.Values.SelectMany(values => values));
                    else if (!string.IsNullOrWhiteSpace(detail))
                        message = detail;
                    else if (!string.IsNullOrWhiteSpace(title))
                        message = title;
                }
            }
            catch (JsonException)
            {
                // El cuerpo no era ProblemDetails: se conserva el mensaje genérico.
            }
        }

        return new ApiException(message, response.StatusCode, errors);
    }

    private static string? ReadString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
