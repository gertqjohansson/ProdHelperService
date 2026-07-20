namespace ProdHelperService.Contracts.EquipmentLink;

public class DeleteEquipmentLinkRequest
{
    public int Id { get; set; }
    public DateTime ActionTimeUtc { get; set; }
    public string MadeByUser { get; set; } = string.Empty;
}
