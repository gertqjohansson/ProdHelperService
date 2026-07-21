using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace ProdHelperService.UsageTracking;

// Typed HttpClient (BaseAddress configured via AddHttpClient<TokenTrackingClient> in Program.cs,
// same pattern as LibreTranslateService/MyMemoryTranslationService) posting to the central
// ProdHelperTokensService. This is a plain outbound HTTPS call - unlike the webapp's inbound path,
// no Azure Relay tunnel is needed here.
public class TokenTrackingClient(HttpClient httpClient, IOptions<TokenTrackingOptions> options)
{
    public async Task<bool> ReportUsageAsync(long tokensUsed, CancellationToken cancellationToken)
    {
        TokenTrackingOptions opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.BaseUrl) || string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            Console.WriteLine("[TokenTracking] TokenTracking:BaseUrl/ApiKey not configured - skipping usage report.");
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/TokenUsage/Increment")
            {
                Content = JsonContent.Create(new { tokensUsed }),
            };
            request.Headers.Add("X-Api-Key", opts.ApiKey);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[TokenTracking] Usage report failed with HTTP {(int)response.StatusCode}.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenTracking] Usage report failed: {ex.Message}");
            return false;
        }
    }
}
