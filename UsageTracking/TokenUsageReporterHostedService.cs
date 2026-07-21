using Microsoft.Extensions.Options;

namespace ProdHelperService.UsageTracking;

// Same BackgroundService shape as RelayListenerHostedService. On each tick, drains UsageCounter
// and reports it to the central ProdHelperTokensService. If the report fails (service down,
// network error), the count is added back rather than lost, so it's included in the next
// successful report instead of silently disappearing during an outage.
public class TokenUsageReporterHostedService(UsageCounter counter, TokenTrackingClient client, IOptions<TokenTrackingOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int intervalMinutes = options.Value.IntervalMinutes > 0 ? options.Value.IntervalMinutes : 10;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            long tokensUsed = counter.TakeAndReset();
            if (tokensUsed == 0)
            {
                continue; // nothing happened this period - no point reporting a no-op
            }

            bool success = await client.ReportUsageAsync(tokensUsed, stoppingToken);
            if (!success)
            {
                counter.AddBack(tokensUsed);
            }
        }
    }
}
