using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IExamenesApi
{
    Task<IReadOnlyList<ExamenDto>> GetAllAsync(int? idMateria, int? idPeriodo, CancellationToken ct);

    Task<IReadOnlyList<InscripcionExamenDto>> GetInscripcionesAsync(int idExamen, CancellationToken ct);

    Task<ExamenDto> CreateAsync(CrearExamenRequest request, CancellationToken ct);

    Task<ExamenDto> UpdateAsync(int id, ActualizarExamenRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

public sealed class ExamenesApi(HttpClient http) : IExamenesApi
{
    public async Task<IReadOnlyList<ExamenDto>> GetAllAsync(
        int? idMateria,
        int? idPeriodo,
        CancellationToken ct)
    {
        var url = $"api/examenes{ApiHttp.Query(("idMateria", idMateria), ("idPeriodo", idPeriodo))}";
        using var response = await http.GetAsync(url, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<ExamenDto>>(response, ct);
    }

    public async Task<IReadOnlyList<InscripcionExamenDto>> GetInscripcionesAsync(
        int idExamen,
        CancellationToken ct)
    {
        using var response = await http.GetAsync($"api/examenes/{idExamen}/inscripciones", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<InscripcionExamenDto>>(response, ct);
    }

    public async Task<ExamenDto> CreateAsync(CrearExamenRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/examenes", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<ExamenDto>(response, ct);
    }

    public async Task<ExamenDto> UpdateAsync(int id, ActualizarExamenRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"api/examenes/{id}", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<ExamenDto>(response, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/examenes/{id}", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);
    }
}
