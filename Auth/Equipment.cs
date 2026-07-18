namespace ProdHelperService.Auth;

// Maps to the pre-existing [Equipment] table (not owned by our EF migrations —
// see ApplicationDbContext.OnModelCreating's ExcludeFromMigrations calls).
public class Equipment
{
    public int Id { get; set; }
    public int? ParentId { get; set; } // null = top-level/root (self-referencing FK, so 0 isn't valid)
    public string? ExternalCode { get; set; }
    public bool? IsOee { get; set; }
    public bool IsPlannable { get; set; }
    public string? ColorCode { get; set; }
    public int? EquipmentCategoryId { get; set; }
    public bool? IsDeleted { get; set; }
    public bool? UseEconomy { get; set; }
    public DateTime? DateOfPurchase { get; set; }
    public double? Price { get; set; }
    public int? DepreciationPeriod { get; set; }
    public bool? UseNotification { get; set; }
    public DateTime? NotificationDate { get; set; }
    public string? Notification { get; set; }
}
