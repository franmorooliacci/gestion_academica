using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IAsignacionesDocentesApi
{
    Task<IReadOnlyList<AsignacionDocenteDto>> GetAllAsync(int? idMateria, int? idPersona, CancellationToken ct);

    Task<AsignacionDocenteDto> CreateAsync(CrearAsignacionDocenteRequest request, CancellationToken ct);

    Task<AsignacionDocenteDto> UpdateAsync(int id, ActualizarAsignacionDocenteRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);
}

public sealed class AsignacionesDocentesApi(HttpClient http) : IAsignacionesDocentesApi
{
    public async Task<IReadOnlyList<AsignacionDocenteDto>> GetAllAsync(
        int? idMateria,
        int? idPersona,
        CancellationToken ct)
    {
        var url = $"api/asignaciones-docentes{ApiHttp.Query(("idMateria", idMateria), ("idPersona", idPersona))}";
        using var response = await http.GetAsync(url, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<AsignacionDocenteDto>>(response, ct);
    }

    public async Task<AsignacionDocenteDto> CreateAsync(CrearAsignacionDocenteRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(
            "api/asignaciones-docentes",
            request,
            ApiJson.Options,
            ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<AsignacionDocenteDto>(response, ct);
    }

    public async Task<AsignacionDocenteDto> UpdateAsync(
        int id,
        ActualizarAsignacionDocenteRequest request,
        CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync(
            $"api/asignaciones-docentes/{id}",
            request,
            ApiJson.Options,
            ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<AsignacionDocenteDto>(response, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/asignaciones-docentes/{id}", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);
    }
}
