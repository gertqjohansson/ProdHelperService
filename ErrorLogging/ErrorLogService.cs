using ProdHelperService.Auth;

namespace ProdHelperService.ErrorLogging;

// Only usable from within the ASP.NET Core request pipeline (relies on the scoped
// ApplicationDbContext DI provides per-request) - see Program.cs's global exception handler,
// the only current caller.
public class ErrorLogService(ApplicationDbContext db) : IErrorLogService
{
    public async Task LogAsync(string section, Exception exception, CancellationToken cancellationToken = default)
    {
        try
        {
            db.ErrorLogs.Add(new ErrorLog
            {
                ErrorDate = DateTime.UtcNow,
                Section = section,
                ErrorMessage = exception.InnerException?.Message ?? exception.Message,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception loggingException)
        {
            // Never let a failure to write the error log itself take down the error handler.
            Console.WriteLine($"[ErrorLog] Failed to write error log entry: {loggingException.Message}");
        }
    }
}
