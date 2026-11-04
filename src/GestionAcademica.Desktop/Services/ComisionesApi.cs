using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IComisionesApi
{
    Task<IReadOnlyList<ComisionDto>> GetAllAsync(int? idMateria, int? idPeriodo, CancellationToken ct);

    Task<ComisionDto> CreateAsync(CrearComisionRequest request, CancellationToken ct);

    Task<ComisionDto> UpdateAsync(int id, ActualizarComisionRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

public sealed class ComisionesApi(HttpClient http) : IComisionesApi
{
    public async Task<IReadOnlyList<ComisionDto>> GetAllAsync(
        int? idMateria,
        int? idPeriodo,
        CancellationToken ct)
    {
        var url = $"api/comisiones{ApiHttp.Query(("idMateria", idMateria), ("idPeriodo", idPeriodo))}";
        using var response = await http.GetAsync(url, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<ComisionDto>>(response, ct);
    }

    public async Task<ComisionDto> CreateAsync(CrearComisionRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/comisiones", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<ComisionDto>(response, ct);
    }

    public async Task<ComisionDto> UpdateAsync(int id, ActualizarComisionRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"api/comisiones/{id}", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<ComisionDto>(response, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/comisiones/{id}", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);
    }
}
