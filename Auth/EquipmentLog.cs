namespace ProdHelperService.Auth;

// A freeform dated note attached to an Equipment item. CreatedBy is a snapshot of the author's
// display name/email taken at creation time - it is never changed by later edits. DateTimeUtc is
// set at creation and refreshed on every edit ("created or updated").
public class EquipmentLog
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string LogText { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime DateTimeUtc { get; set; }
}
