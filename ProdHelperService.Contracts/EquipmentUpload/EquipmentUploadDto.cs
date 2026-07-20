namespace ProdHelperService.Contracts.EquipmentUpload;

public class EquipmentUploadDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
