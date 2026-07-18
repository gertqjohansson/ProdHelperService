namespace ProdHelperService.Auth;

// Maps to the pre-existing [Language] table.
public class Language
{
    public string IsoCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool? IsFallback { get; set; }
}
