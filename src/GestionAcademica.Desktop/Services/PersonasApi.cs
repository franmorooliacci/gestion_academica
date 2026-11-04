using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IPersonasApi
{
    Task<IReadOnlyList<PersonaDto>> GetAllAsync(string? search, CancellationToken ct);

    Task<PersonaDto> CreateAsync(CrearPersonaRequest request, CancellationToken ct);

    Task<PersonaDto> UpdateAsync(int id, ActualizarPersonaRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

public sealed class PersonasApi(HttpClient http) : IPersonasApi
{
    public async Task<IReadOnlyList<PersonaDto>> GetAllAsync(string? search, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/personas"
            : $"api/personas?search={Uri.EscapeDataString(search)}";

        using var response = await http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<List<PersonaDto>>(ApiJson.Options, ct) ?? [];
    }

    public async Task<PersonaDto> CreateAsync(CrearPersonaRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/personas", request, ApiJson.Options, ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<PersonaDto>(ApiJson.Options, ct)
            ?? throw new ApiException("La API devolvió una persona vacía.", response.StatusCode);
    }

    public async Task<PersonaDto> UpdateAsync(int id, ActualizarPersonaRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"api/personas/{id}", request, ApiJson.Options, ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<PersonaDto>(ApiJson.Options, ct)
            ?? throw new ApiException("La API devolvió una persona vacía.", response.StatusCode);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/personas/{id}", ct);
        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponseAsync(response, ct);
    }
}
