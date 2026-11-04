using System.Net.Http;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IInscripcionesComisionApi
{
    Task<IReadOnlyList<InscripcionComisionDto>> GetAllAsync(int? idComision, CancellationToken ct);
}

public sealed class InscripcionesComisionApi(HttpClient http) : IInscripcionesComisionApi
{
    public async Task<IReadOnlyList<InscripcionComisionDto>> GetAllAsync(
        int? idComision,
        CancellationToken ct)
    {
        var url = $"api/inscripciones-comision{ApiHttp.Query(("idComision", idComision))}";
        using var response = await http.GetAsync(url, ct);
        await ApiHttp.EnsureSuccessAsync(response, ct);

        return await ApiHttp.ReadAsync<List<InscripcionComisionDto>>(response, ct);
    }
}
