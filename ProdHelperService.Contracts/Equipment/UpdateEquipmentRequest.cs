namespace ProdHelperService.Contracts.Equipment;

public class UpdateEquipmentRequest
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExternalCode { get; set; }
    public bool? IsOee { get; set; }
    public bool IsPlannable { get; set; }
    public string? ColorCode { get; set; }
    public int EquipmentCategoryId { get; set; } // 0 = not selected
    public string? LanguageIsoCode { get; set; }
    public bool? UseEconomy { get; set; }
    public DateTime? DateOfPurchase { get; set; }
    public double? Price { get; set; }
    public int? DepreciationPeriod { get; set; }
    public bool? UseNotification { get; set; }
    public DateTime? NotificationDate { get; set; }
    public string? Notification { get; set; }
    public DateTime ActionTimeUtc { get; set; }
    public string MadeByUser { get; set; } = string.Empty;
}
