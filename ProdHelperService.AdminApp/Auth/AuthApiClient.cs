using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

// Wraps the shared HttpClient (same instance MainForm uses for Oee/Planner)
// for ProdHelperService's Auth/* endpoints.
public class AuthApiClient(HttpClient httpClient)
{
    public Task<RegisterResponse> RegisterAsync(RegisterRequest request) =>
        SendAsync<RegisterResponse>(ApiRoutes.AuthRegister, request, accessToken: null);

    public Task<HasUsersResponse> HasUsersAsync() =>
        SendAsync<HasUsersResponse>(ApiRoutes.AuthHasUsers, body: null, accessToken: null);

    public Task<LoginResponse> LoginAsync(LoginRequest request) =>
        SendAsync<LoginResponse>(ApiRoutes.AuthLogin, request, accessToken: null);

    public Task<TokenResponse> VerifyMfaAsync(VerifyMfaRequest request) =>
        SendAsync<TokenResponse>(ApiRoutes.AuthVerifyMfa, request, accessToken: null);

    public Task<TokenResponse> RefreshAsync(RefreshRequest request) =>
        SendAsync<TokenResponse>(ApiRoutes.AuthRefresh, request, accessToken: null);

    public Task LogoutAsync(LogoutRequest request, string accessToken) =>
        SendAsync(ApiRoutes.AuthLogout, request, accessToken);

    public Task<AuthenticatorSetupResponse> SetupAuthenticatorAsync(string accessToken) =>
        SendAsync<AuthenticatorSetupResponse>(ApiRoutes.AuthMfaAuthenticatorSetup, body: null, accessToken);

    public Task<EnableAuthenticatorResponse> EnableAuthenticatorAsync(EnableAuthenticatorRequest request, string accessToken) =>
        SendAsync<EnableAuthenticatorResponse>(ApiRoutes.AuthMfaAuthenticatorEnable, request, accessToken);

    public Task DisableAuthenticatorAsync(DisableAuthenticatorRequest request, string accessToken) =>
        SendAsync(ApiRoutes.AuthMfaAuthenticatorDisable, request, accessToken);

    private async Task<TResponse> SendAsync<TResponse>(string relativeUrl, object? body, string? accessToken)
    {
        using HttpResponseMessage response = await SendCoreAsync(relativeUrl, body, accessToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private async Task SendAsync(string relativeUrl, object? body, string? accessToken)
    {
        using HttpResponseMessage response = await SendCoreAsync(relativeUrl, body, accessToken);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(string relativeUrl, object? body, string? accessToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
        if (body is not null) message.Content = JsonContent.Create(body);
        if (accessToken is not null) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await httpClient.SendAsync(message);
        if (response.IsSuccessStatusCode) return response;

        AuthErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
        }
        catch
        {
            // Bare status with no JSON body (e.g. a 401 from [Authorize] with
            // no controller code involved) — fall back to a generic message.
        }

        throw new AuthApiException(
            error?.Code ?? "RequestFailed",
            error?.Message ?? $"Request failed with HTTP {(int)response.StatusCode}.",
            response.StatusCode);
    }
}
