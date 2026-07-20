namespace ProdHelperService.Contracts.Equipment;

public class DeleteEquipmentRequest
{
    public int Id { get; set; }
    public DateTime ActionTimeUtc { get; set; }
    public string MadeByUser { get; set; } = string.Empty;
}
