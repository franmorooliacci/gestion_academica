using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IMateriasApi
{
    Task<IReadOnlyList<MateriaDto>> GetAllAsync(int? idCarrera, CancellationToken ct);

    Task<MateriaDto> CreateAsync(CrearMateriaRequest request, CancellationToken ct);

    Task<MateriaDto> UpdateAsync(int id, ActualizarMateriaRequest request, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<CorrelatividadDto>> GetCorrelatividadesAsync(int idMateria, CancellationToken ct);

    Task<CorrelatividadDto> AddCorrelatividadAsync(
        int idMateria,
        CrearCorrelatividadRequest request,
        CancellationToken ct);

    Task DeleteCorrelatividadAsync(int idMateria, int idCorrelatividad, CancellationToken ct);
}

public sealed class MateriasApi(HttpClient http) : IMateriasApi
{
    public async Task<IReadOnlyList<MateriaDto>> GetAllAsync(int? idCarrera, CancellationToken ct)
    {
        var url = $"api/materias{ApiHttp.Query(("idCarrera", idCarrera))}";
        using var response = await http.GetAsync(url, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<MateriaDto>>(response, ct);
    }

    public async Task<MateriaDto> CreateAsync(CrearMateriaRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/materias", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<MateriaDto>(response, ct);
    }

    public async Task<MateriaDto> UpdateAsync(int id, ActualizarMateriaRequest request, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"api/materias/{id}", request, ApiJson.Options, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<MateriaDto>(response, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var response = await http.DeleteAsync($"api/materias/{id}", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);
    }

    public async Task<IReadOnlyList<CorrelatividadDto>> GetCorrelatividadesAsync(int idMateria, CancellationToken ct)
    {
        using var response = await http.GetAsync($"api/materias/{idMateria}/correlatividades", ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<CorrelatividadDto>>(response, ct);
    }

    public async Task<CorrelatividadDto> AddCorrelatividadAsync(
        int idMateria,
        CrearCorrelatividadRequest request,
        CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(
            $"api/materias/{idMateria}/correlatividades",
            request,
            ApiJson.Options,
            ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<CorrelatividadDto>(response, ct);
    }

    public async Task DeleteCorrelatividadAsync(int idMateria, int idCorrelatividad, CancellationToken ct)
    {
        using var response = await http.DeleteAsync(
            $"api/materias/{idMateria}/correlatividades/{idCorrelatividad}",
            ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);
    }
}
