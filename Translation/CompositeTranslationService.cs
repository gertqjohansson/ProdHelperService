namespace ProdHelperService.Translation;

// The DI-registered ITranslationService: tries LibreTranslate (self-hosted, private, unlimited)
// first, and only calls the MyMemory fallback when LibreTranslate can't handle the language pair
// (e.g. Croatian/Serbian, which have no Argos model) or is unreachable.
public class CompositeTranslationService(LibreTranslateService primary, MyMemoryTranslationService fallback, ILogger<CompositeTranslationService> logger)
    : ITranslationService
{
    public async Task<string> TranslateAsync(string text, string fromLanguageIsoCode, string toLanguageIsoCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text) || string.Equals(fromLanguageIsoCode, toLanguageIsoCode, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        try
        {
            return await primary.TranslateAsync(text, fromLanguageIsoCode, toLanguageIsoCode, cancellationToken);
        }
        catch (TranslationUnavailableException ex)
        {
            logger.LogInformation(ex, "LibreTranslate unavailable for {From}->{To}, falling back to MyMemory.", fromLanguageIsoCode, toLanguageIsoCode);
            return await fallback.TranslateAsync(text, fromLanguageIsoCode, toLanguageIsoCode, cancellationToken);
        }
    }
}
