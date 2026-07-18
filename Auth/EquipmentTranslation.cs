namespace ProdHelperService.Auth;

// Maps to the pre-existing [EquipmentTranslation] table — one row per
// (EquipmentId, LanguageIsoCode), holding that language's display name.
public class EquipmentTranslation
{
    public int EquipmentId { get; set; }
    public string LanguageIsoCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
