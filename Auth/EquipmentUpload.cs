namespace ProdHelperService.Auth;

// A file attached to an Equipment item. Unlike Equipment/EquipmentTranslation/etc., this table is
// brand new and fully owned by EF migrations (see ApplicationDbContext.OnModelCreating - no
// ExcludeFromMigrations here). One physical file lives on disk per row, at
// {Storage:UploadPath}\Equipments\{EquipmentId}\{FileName} (see Storage/IFileStorageService.cs).
public class EquipmentUpload
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
