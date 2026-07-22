namespace ProdHelperService.Contracts.ShiftScheduleVersion;

public class CreateShiftScheduleVersionRequest
{
    public int EquipmentId { get; set; }
    public DateTime StartDate { get; set; }
    public int DaysInScedule { get; set; }
}
