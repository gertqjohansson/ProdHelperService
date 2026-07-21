namespace ProdHelperTokensService;

// Provisioned manually (one INSERT per customer as they're onboarded) - there is no self-service
// registration flow. ApiKey is the credential a customer's ProdHelperService install authenticates
// with when reporting usage.
public class Customer
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }

    // Denormalized running total, updated alongside every new TokenUsageEntry in the same write -
    // cheap to maintain and avoids a schema change later if/when a usage-reporting endpoint is
    // added. TokenUsageEntries remains the source of truth/audit trail.
    public long TotalTokensUsed { get; set; }
}
