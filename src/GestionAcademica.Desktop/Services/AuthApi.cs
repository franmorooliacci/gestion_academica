using System.Net.Http;
using System.Net.Http.Json;
using GestionAcademica.Shared.Contracts.DTOs;

namespace GestionAcademica.Desktop.Services;

public interface IAuthApi
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);

    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct);
}

public sealed class AuthApi(HttpClient http) : IAuthApi
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/auth/login", request, ApiJson.Options, ct);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponseAsync(response, ct);

        return await ReadLoginAsync(response, "login", ct);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/auth/refresh", request, ApiJson.Options, ct);

        if (!response.IsSuccessStatusCode)
            throw await ApiException.FromResponseAsync(response, ct);

        return await ReadLoginAsync(response, "refresh", ct);
    }

    private static async Task<LoginResponse> ReadLoginAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<LoginResponse>(ApiJson.Options, ct)
            ?? throw new ApiException(
                $"La API devolvió una respuesta de {operation} vacía.",
                response.StatusCode);
    }
}
