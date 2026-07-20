namespace ProdHelperService.Auth;

// Maps to the pre-existing [ErrorLog] table (not owned by our EF migrations - see
// ApplicationDbContext.OnModelCreating's ExcludeFromMigrations call). Written by
// ErrorLogging/ErrorLogService.cs whenever an unhandled exception reaches the global exception
// handler in Program.cs.
public class ErrorLog
{
    public int Id { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Section { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
