namespace ProdHelperService.Contracts.EquipmentUpload;

public class UploadEquipmentFileRequest
{
    public int EquipmentId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentBase64 { get; set; } = string.Empty;
    public bool Overwrite { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string MadeByUser { get; set; } = string.Empty;
}
