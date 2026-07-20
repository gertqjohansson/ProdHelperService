namespace ProdHelperService.Contracts.EquipmentLog;

public class UpdateEquipmentLogRequest
{
    public int Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string LogText { get; set; } = string.Empty;
    public DateTime DateTimeUtc { get; set; }
}
