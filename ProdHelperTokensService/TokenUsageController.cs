using Microsoft.AspNetCore.Mvc;
using ProdHelperTokensService.Contracts;

namespace ProdHelperTokensService;

[ApiController]
[Route("api/[controller]")]
public class TokenUsageController(TokensDbContext db) : ControllerBase
{
    [HttpPost("Increment")]
    public async Task<IActionResult> Increment(IncrementTokenUsageRequest request)
    {
        if (request.TokensUsed < 0)
        {
            return BadRequest(new ApiErrorResponse { Code = "TokensUsedInvalid", Message = "TokensUsed cannot be negative." });
        }

        var customer = (Customer)HttpContext.Items[ApiKeyAuthFilter.CustomerItemKey]!;

        db.TokenUsageEntries.Add(new TokenUsageEntry
        {
            CustomerId = customer.Id,
            RecordedUtc = DateTime.UtcNow,
            TokensUsed = request.TokensUsed,
        });
        customer.TotalTokensUsed += request.TokensUsed;

        await db.SaveChangesAsync();

        return Ok(new IncrementTokenUsageResponse { TotalTokensUsed = customer.TotalTokensUsed });
    }
}
