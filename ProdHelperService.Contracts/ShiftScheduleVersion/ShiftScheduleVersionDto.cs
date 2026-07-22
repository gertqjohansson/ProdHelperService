namespace ProdHelperService.Contracts.ShiftScheduleVersion;

public class ShiftScheduleVersionDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public DateTime StartDate { get; set; }
    public int DaysInScedule { get; set; }
}
