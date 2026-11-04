using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IRolesApi
{
    Task<IReadOnlyList<RolDto>> GetAllAsync(CancellationToken ct);
}

public sealed class RolesApi(HttpClient http) : IRolesApi
{
    public async Task<IReadOnlyList<RolDto>> GetAllAsync(CancellationToken ct)
    {
        using var response = await http.GetAsync("api/roles", ct);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponseAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<List<RolDto>>(ApiJson.Options, ct) ?? [];
    }
}
