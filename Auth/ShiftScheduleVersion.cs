namespace ProdHelperService.Auth;

// A shift schedule ("calendar") generated for a piece of equipment, starting on StartDate and
// covering DaysInScedule days. Multiple versions can exist per equipment (re-planning) - existence
// of any non-deleted row for an EquipmentId is what the client shows as the "has calendar" tree
// icon. Column/property names match the pre-existing table exactly (including its "Scedule"
// spelling, consistent with Equipment.SchiftParentId elsewhere in this codebase).
public class ShiftScheduleVersion
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public DateTime StartDate { get; set; }
    public int DaysInScedule { get; set; }
    public bool IsDeleted { get; set; }
}
