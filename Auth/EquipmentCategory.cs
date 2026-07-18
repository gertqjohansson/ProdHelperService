namespace ProdHelperService.Auth;

// Maps to the pre-existing [EquipmentCategory] table - just an Id, the name
// lives entirely in EquipmentCategoryTranslation (see EquipmentCategoryTranslation.cs).
public class EquipmentCategory
{
    public int Id { get; set; }
    public string? ColorCode { get; set; }
}
