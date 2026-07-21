namespace ProdHelperService.UsageTracking;

public class TokenTrackingOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } = 10;
}
