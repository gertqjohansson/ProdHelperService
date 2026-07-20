namespace ProdHelperService.Contracts.EquipmentUpload;

public class DownloadEquipmentFileResponse
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string ContentBase64 { get; set; } = string.Empty;
}
