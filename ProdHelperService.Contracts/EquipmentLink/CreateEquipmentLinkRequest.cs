namespace ProdHelperService.Contracts.EquipmentLink;

public class CreateEquipmentLinkRequest
{
    public int EquipmentId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDocument { get; set; }
    public DateTime ActionTimeUtc { get; set; }
    public string MadeByUser { get; set; } = string.Empty;
}
