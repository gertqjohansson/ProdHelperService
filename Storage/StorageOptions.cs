namespace ProdHelperService.Storage;

public class StorageOptions
{
    public string UploadPath { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB default
}
