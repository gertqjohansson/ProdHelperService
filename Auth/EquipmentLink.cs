namespace ProdHelperService.Auth;

// A link attached to an Equipment item - either a webpage to open, or a path to a document
// stored elsewhere (e.g. SharePoint) to download. Unlike EquipmentUpload, no file content is
// ever stored or proxied by this app; only the URL/path itself is persisted.
public class EquipmentLink
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDocument { get; set; }
}
