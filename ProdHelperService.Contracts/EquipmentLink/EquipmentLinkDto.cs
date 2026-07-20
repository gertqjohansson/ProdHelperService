namespace ProdHelperService.Contracts.EquipmentLink;

public class EquipmentLinkDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDocument { get; set; }
}
