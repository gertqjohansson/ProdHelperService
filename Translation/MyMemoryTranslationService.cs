using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace ProdHelperService.Translation;

// Fallback translation provider - see TranslationSettings for why. Free, no API key, no signup.
public class MyMemoryTranslationService(HttpClient httpClient, IOptions<TranslationSettings> options, ILogger<MyMemoryTranslationService> logger)
    : ITranslationService
{
    public async Task<string> TranslateAsync(string text, string fromLanguageIsoCode, string toLanguageIsoCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (string.Equals(fromLanguageIsoCode, toLanguageIsoCode, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        string from = NormalizeLanguageCode(fromLanguageIsoCode);
        string to = NormalizeLanguageCode(toLanguageIsoCode);
        string? contactEmail = options.Value.MyMemoryContactEmail;
        string url = $"get?q={Uri.EscapeDataString(text)}&langpair={from}|{to}"
            + (string.IsNullOrWhiteSpace(contactEmail) ? "" : $"&de={Uri.EscapeDataString(contactEmail)}");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "MyMemory request failed for {From}->{To}.", fromLanguageIsoCode, toLanguageIsoCode);
            throw new TranslationUnavailableException("MyMemory translation service is unreachable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("MyMemory returned {Status} for {From}->{To}.", response.StatusCode, fromLanguageIsoCode, toLanguageIsoCode);
            throw new TranslationUnavailableException($"MyMemory returned {(int)response.StatusCode}.");
        }

        MyMemoryResponse? result = await response.Content.ReadFromJsonAsync<MyMemoryResponse>(cancellationToken: cancellationToken);
        if (result?.ResponseStatus != 200 || string.IsNullOrEmpty(result.ResponseData?.TranslatedText))
        {
            logger.LogWarning(
                "MyMemory could not translate {From}->{To}: status={Status} details={Details}",
                fromLanguageIsoCode, toLanguageIsoCode, result?.ResponseStatus, result?.ResponseDetails);
            throw new TranslationUnavailableException("MyMemory could not translate the requested language pair.");
        }

        return result.ResponseData.TranslatedText;
    }

    private static string NormalizeLanguageCode(string isoCode) => isoCode.Split('-')[0].ToLowerInvariant();

    private record MyMemoryResponse(MyMemoryResponseData? ResponseData, int ResponseStatus, string? ResponseDetails);

    private record MyMemoryResponseData(string TranslatedText);
}
