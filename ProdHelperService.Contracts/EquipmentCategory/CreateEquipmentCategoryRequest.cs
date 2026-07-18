namespace ProdHelperService.Contracts.EquipmentCategory;

public class CreateEquipmentCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public string? LanguageIsoCode { get; set; }
}
