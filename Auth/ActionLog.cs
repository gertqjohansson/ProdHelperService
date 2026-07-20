namespace ProdHelperService.Auth;

// Maps to the pre-existing [ActionLog] table (not owned by our EF migrations - see
// ApplicationDbContext.OnModelCreating's ExcludeFromMigrations call). Written by
// ActionLogging/ActionLogService.cs for every Create/Update/Delete on Equipment,
// EquipmentUpload and EquipmentLink.
public class ActionLog
{
    public int Id { get; set; }
    public DateTime ActionTime { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string MadeByUser { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string NewValues { get; set; } = string.Empty;
}
