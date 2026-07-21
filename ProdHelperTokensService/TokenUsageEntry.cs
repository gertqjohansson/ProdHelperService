namespace ProdHelperTokensService;

// One row per 10-minute usage report from a customer's ProdHelperService install. Append-only -
// this is the audit trail behind Customer.TotalTokensUsed.
public class TokenUsageEntry
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime RecordedUtc { get; set; }
    public int TokensUsed { get; set; }
}
