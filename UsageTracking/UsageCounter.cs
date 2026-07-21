namespace ProdHelperService.UsageTracking;

// Thread-safe counter incremented once per real API request (see the middleware registered in
// Program.cs) and drained every few minutes by TokenUsageReporterHostedService.
public class UsageCounter
{
    private long _count;

    public void Increment() => Interlocked.Increment(ref _count);

    // Reads the current count and resets it to zero in one atomic step, so concurrent increments
    // during the read are never lost.
    public long TakeAndReset() => Interlocked.Exchange(ref _count, 0);

    // Adds a previously-taken count back - used when a report to the central service fails, so
    // that usage isn't silently lost during an outage.
    public void AddBack(long count) => Interlocked.Add(ref _count, count);
}
