using System.Net.Http.Json;

namespace ProdHelperService.Translation;

public class LibreTranslateService(HttpClient httpClient, ILogger<LibreTranslateService> logger) : ITranslationService
{
    public async Task<string> TranslateAsync(string text, string fromLanguageIsoCode, string toLanguageIsoCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Same language on both sides (the app always passes its own exact SUPPORTED_LANGUAGES
        // codes here) - no call needed.
        if (string.Equals(fromLanguageIsoCode, toLanguageIsoCode, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        var payload = new
        {
            q = text,
            source = NormalizeLanguageCode(fromLanguageIsoCode),
            target = NormalizeLanguageCode(toLanguageIsoCode),
            format = "text",
        };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("translate", payload, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "LibreTranslate request failed for {From}->{To}.", fromLanguageIsoCode, toLanguageIsoCode);
            throw new TranslationUnavailableException("Translation service is unreachable.");
        }

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("LibreTranslate returned {Status} for {From}->{To}: {Body}", response.StatusCode, fromLanguageIsoCode, toLanguageIsoCode, body);
            throw new TranslationUnavailableException($"Translation service returned {(int)response.StatusCode}.");
        }

        LibreTranslateResponse? result = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(cancellationToken: cancellationToken);
        return result?.TranslatedText ?? text;
    }

    private static string NormalizeLanguageCode(string isoCode) => isoCode.Split('-')[0].ToLowerInvariant();

    private record LibreTranslateResponse(string TranslatedText);
}
