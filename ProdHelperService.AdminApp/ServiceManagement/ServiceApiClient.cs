using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.Service;

namespace ProdHelperService.AdminApp;

// Wraps the shared HttpClient (same instance AuthApiClient/MainForm use) for
// ProdHelperService's Service/* endpoints. Kept separate from AuthApiClient
// rather than sharing a base class, since it's the only other typed client so far.
public class ServiceApiClient(HttpClient httpClient)
{
    public Task<GetServiceInfoResponse> GetInfoAsync(string accessToken) =>
        SendAsync<GetServiceInfoResponse>(ApiRoutes.ServiceGetInfo, body: null, accessToken);

    public Task<UpdatePortResponse> UpdatePortAsync(UpdatePortRequest request, string accessToken) =>
        SendAsync<UpdatePortResponse>(ApiRoutes.ServiceUpdatePort, request, accessToken);

    // Nullable token: only ever called with a real token today, from ServiceRegistrationForm
    // post-login. Kept nullable rather than tightened to match GetInfoAsync/UpdatePortAsync
    // above, since this and RegisterAsync originally supported a pre-login caller too (see
    // IamAliveAsync below for the endpoint that now actually needs that).
    public Task<ServiceRegistrationStatusResponse> GetRegistrationStatusAsync(string? accessToken) =>
        SendAsync<ServiceRegistrationStatusResponse>(ApiRoutes.ServiceGetRegistrationStatus, body: null, accessToken);

    public Task<ServiceActionResponse> RegisterAsync(string? accessToken) =>
        SendAsync<ServiceActionResponse>(ApiRoutes.ServiceRegister, body: null, accessToken);

    public Task<ServiceActionResponse> UnregisterAsync(string accessToken) =>
        SendAsync<ServiceActionResponse>(ApiRoutes.ServiceUnregister, body: null, accessToken);

    public Task<ServiceActionResponse> StartAsync(string accessToken) =>
        SendAsync<ServiceActionResponse>(ApiRoutes.ServiceStart, body: null, accessToken);

    public Task<ServiceActionResponse> StopAsync(string accessToken) =>
        SendAsync<ServiceActionResponse>(ApiRoutes.ServiceStop, body: null, accessToken);

    // Nullable token: called from Program.cs (EnsureServiceReachableAsync, before any session
    // exists) and from ServiceConfigForm's "Try Service" button, which must work even when
    // nothing else about the session is established yet. The backend endpoint is
    // [AllowAnonymous] specifically to support this.
    public Task<ServiceAliveResponse> IamAliveAsync(string? accessToken) =>
        SendAsync<ServiceAliveResponse>(ApiRoutes.ServiceIamAlive, body: null, accessToken);

    private async Task<TResponse> SendAsync<TResponse>(string relativeUrl, object? body, string? accessToken)
    {
        using HttpResponseMessage response = await SendCoreAsync(relativeUrl, body, accessToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
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
            // Bare status with no JSON body — fall back to a generic message.
        }

        throw new AuthApiException(
            error?.Code ?? "RequestFailed",
            error?.Message ?? $"Request failed with HTTP {(int)response.StatusCode}.",
            response.StatusCode);
    }
}
