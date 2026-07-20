namespace ProdHelperService.Contracts.Equipment;

public class SaveEquipmentCommentRequest
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public string? LanguageIsoCode { get; set; }
}
