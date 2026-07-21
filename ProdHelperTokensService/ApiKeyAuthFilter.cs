using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ProdHelperTokensService.Contracts;

namespace ProdHelperTokensService;

// Applied globally (see Program.cs's AddControllers(options => options.Filters.Add<...>())) since
// every endpoint in this service is server-to-server and must be authenticated - there are no
// public/anonymous routes here. Resolves the calling Customer from the X-Api-Key header and
// stashes it on HttpContext.Items for the controller to use, rather than re-querying it there.
public class ApiKeyAuthFilter(TokensDbContext db) : IAsyncActionFilter
{
    public const string CustomerItemKey = "Customer";
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues) ||
            string.IsNullOrWhiteSpace(apiKeyValues.ToString()))
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = "ApiKeyMissing",
                Message = $"The {ApiKeyHeaderName} header is required.",
            });
            return;
        }

        string apiKey = apiKeyValues.ToString();
        Customer? customer = await db.Customers.FirstOrDefaultAsync(c => c.ApiKey == apiKey && c.IsActive);
        if (customer is null)
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = "ApiKeyInvalid",
                Message = "Invalid or inactive API key.",
            });
            return;
        }

        context.HttpContext.Items[CustomerItemKey] = customer;
        await next();
    }
}
