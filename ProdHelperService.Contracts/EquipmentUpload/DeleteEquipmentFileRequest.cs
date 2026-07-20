namespace ProdHelperService.Contracts.EquipmentUpload;

public class DeleteEquipmentFileRequest
{
    public int Id { get; set; }
    public DateTime ActionTimeUtc { get; set; }
    public string MadeByUser { get; set; } = string.Empty;
}
