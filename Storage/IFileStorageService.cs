namespace ProdHelperService.Storage;

// Backs EquipmentUploadController's on-disk file storage - one subfolder per equipment under
// {Storage:UploadPath}\Equipments\, so files with the same name on different equipment items
// never collide.
public interface IFileStorageService
{
    // True once Storage:UploadPath has a value - lets callers return a clear "not configured yet"
    // error up front instead of letting an exception surface from deep inside a file operation.
    bool IsConfigured { get; }

    bool FileExists(int equipmentId, string fileName);
    Task SaveFileAsync(int equipmentId, string fileName, byte[] content);
    Task<byte[]> ReadFileAsync(int equipmentId, string fileName);
    void DeleteFile(int equipmentId, string fileName);

    // Throws if fileName doesn't safely resolve to a single file name (path traversal guard) -
    // callers should run untrusted file names through this before any other storage call.
    string SanitizeFileName(string fileName);
}
