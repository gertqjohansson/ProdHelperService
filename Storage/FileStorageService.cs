using Microsoft.Extensions.Options;

namespace ProdHelperService.Storage;

public class FileStorageService(IOptions<StorageOptions> options) : IFileStorageService
{
    private readonly StorageOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.UploadPath);

    public string SanitizeFileName(string fileName)
    {
        string candidate = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(candidate) || candidate != fileName)
        {
            throw new ArgumentException("Invalid file name.", nameof(fileName));
        }

        if (candidate.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Invalid file name.", nameof(fileName));
        }

        return candidate;
    }

    public bool FileExists(int equipmentId, string fileName) =>
        File.Exists(Path.Combine(GetEquipmentFolder(equipmentId), fileName));

    public async Task SaveFileAsync(int equipmentId, string fileName, byte[] content)
    {
        string path = Path.Combine(GetEquipmentFolder(equipmentId), fileName);
        await File.WriteAllBytesAsync(path, content);
    }

    public Task<byte[]> ReadFileAsync(int equipmentId, string fileName) =>
        File.ReadAllBytesAsync(Path.Combine(GetEquipmentFolder(equipmentId), fileName));

    public void DeleteFile(int equipmentId, string fileName)
    {
        string path = Path.Combine(GetEquipmentFolder(equipmentId), fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    // Resolves and creates {UploadPath}\Equipments\{equipmentId}. UploadPath is only validated
    // here (lazily, on first actual use) rather than at startup, so a not-yet-configured
    // Storage:UploadPath doesn't prevent the app from starting.
    private string GetEquipmentFolder(int equipmentId)
    {
        if (string.IsNullOrWhiteSpace(_options.UploadPath))
        {
            throw new InvalidOperationException("Storage:UploadPath is not configured.");
        }

        string equipmentsFolder = Path.Combine(_options.UploadPath, "Equipments");
        string folder = Path.Combine(equipmentsFolder, equipmentId.ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }
}
