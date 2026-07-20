namespace ProdHelperService.Translation;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string fromLanguageIsoCode, string toLanguageIsoCode, CancellationToken cancellationToken = default);
}
