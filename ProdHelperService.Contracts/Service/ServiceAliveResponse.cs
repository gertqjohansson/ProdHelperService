namespace ProdHelperService.Contracts.Service;

// Minimal payload for Service/IamAlive - the 200 OK is the actual signal (the API is
// reachable); ServerUtcTime is a cheap bonus, not load-bearing today.
public class ServiceAliveResponse
{
    public DateTime ServerUtcTime { get; set; }
}
