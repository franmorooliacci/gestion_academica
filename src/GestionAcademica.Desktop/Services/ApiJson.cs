using System.Text.Json;
using GestionAcademica.Shared.Contracts.Serialization;

namespace GestionAcademica.Desktop.Services;

/// <summary>
/// Opciones JSON de la API: enums como string (regla del contrato) y camelCase de ASP.NET Core.
/// Deben usarse en TODAS las llamadas HTTP del escritorio.
/// </summary>
public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumMemberConverter());
        return options;
    }
}
