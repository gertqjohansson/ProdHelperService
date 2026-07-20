namespace ProdHelperService.Translation;

public class TranslationSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5050";
    public int TimeoutSeconds { get; set; } = 15;

    // MyMemory (https://mymemory.translated.net) is the fallback provider, used only when
    // LibreTranslate doesn't support a language pair (e.g. Croatian/Serbian, which have no Argos
    // Translate model at all) or is unreachable. Free, no signup required. Setting ContactEmail
    // raises MyMemory's daily quota from ~5,000 to ~50,000 words - optional, leave blank to skip.
    public string MyMemoryBaseUrl { get; set; } = "https://api.mymemory.translated.net";
    public string? MyMemoryContactEmail { get; set; }
}
