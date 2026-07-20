namespace ProdHelperService.ErrorLogging;

public interface IErrorLogService
{
    Task LogAsync(string section, Exception exception, CancellationToken cancellationToken = default);
}
