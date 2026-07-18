namespace ProdHelperService.Auth;

// Maps to the pre-existing [EquipmentCategoryTranslation] table - one row per
// (EquipmentCategoryId, LanguageIsoCode), holding that language's display name.
public class EquipmentCategoryTranslation
{
    public int EquipmentCategoryId { get; set; }
    public string LanguageIsoCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
