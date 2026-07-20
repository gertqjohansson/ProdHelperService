namespace ProdHelperService.Contracts.Translation;

public class TranslateRequest
{
    public string Text { get; set; } = string.Empty;
    public string FromLanguageIsoCode { get; set; } = string.Empty;
    public string ToLanguageIsoCode { get; set; } = string.Empty;
}
