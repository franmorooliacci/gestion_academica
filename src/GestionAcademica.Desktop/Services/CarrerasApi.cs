using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface ICarrerasApi
{
    Task<IReadOnlyList<CarreraDto>> GetAllAsync(CancellationToken ct);

    Task<CarreraDto> CreateAsync(CrearCarreraRequest request, CancellationToken ct);

    Task<CarreraDto> UpdateAsync(int id, ActualizarCarreraRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

public sealed class CarrerasApi(HttpClient http) : ICarrerasApi
{
    public async Task<IReadOnlyList<CarreraDto>> GetAllAsync(CancellationToken ct)
    {
        using var response = await http.GetAsync("api/carreras", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<CarreraDto>>(response, ct);
    }

    public async Task<CarreraDto> CreateAsync(CrearCarreraRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/carreras", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<CarreraDto>(response, ct);
    }

    public async Task<CarreraDto> UpdateAsync(int id, ActualizarCarreraRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"api/carreras/{id}", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<CarreraDto>(response, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/carreras/{id}", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);
    }
}
