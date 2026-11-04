using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IPeriodosApi
{
    Task<IReadOnlyList<PeriodoDto>> GetAllAsync(CancellationToken ct);

    Task<PeriodoDto> CreateAsync(CrearPeriodoRequest request, CancellationToken ct);

    Task<PeriodoDto> UpdateAsync(int id, ActualizarPeriodoRequest request, CancellationToken ct);

    Task<PeriodoDto> UpdateVentanasAsync(int id, ActualizarVentanasRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

public sealed class PeriodosApi(HttpClient http) : IPeriodosApi
{
    public async Task<IReadOnlyList<PeriodoDto>> GetAllAsync(CancellationToken ct)
    {
        using var response = await http.GetAsync("api/periodos", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<PeriodoDto>>(response, ct);
    }

    public async Task<PeriodoDto> CreateAsync(CrearPeriodoRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/periodos", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<PeriodoDto>(response, ct);
    }

    public async Task<PeriodoDto> UpdateAsync(int id, ActualizarPeriodoRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"api/periodos/{id}", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<PeriodoDto>(response, ct);
    }

    public async Task<PeriodoDto> UpdateVentanasAsync(int id, ActualizarVentanasRequest request, CancellationToken ct)
    {
        using var response = await http.PatchAsJsonAsync(
            $"api/periodos/{id}/ventanas",
            request,
            ApiJson.Options,
            ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<PeriodoDto>(response, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/periodos/{id}", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);
    }
}
