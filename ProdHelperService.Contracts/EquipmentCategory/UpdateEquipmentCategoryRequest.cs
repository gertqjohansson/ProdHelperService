namespace ProdHelperService.Contracts.EquipmentCategory;

public class UpdateEquipmentCategoryRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public string? LanguageIsoCode { get; set; }
}
