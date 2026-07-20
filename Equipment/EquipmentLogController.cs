using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProdHelperService.Auth;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.EquipmentLog;

namespace ProdHelperService;

[ApiController]
[Authorize(Roles = "Administrator")]
public class EquipmentLogController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost(ApiRoutes.EquipmentLogList)]
    public async Task<IActionResult> List(EquipmentLogListRequest request)
    {
        List<Auth.EquipmentLog> logs = await db.EquipmentLogs
            .Where(x => x.EquipmentId == request.EquipmentId)
            .OrderByDescending(x => x.DateTimeUtc)
            .ToListAsync();

        return Ok(new EquipmentLogListResponse { Items = logs.Select(ToDto).ToList() });
    }

    [HttpPost(ApiRoutes.EquipmentLogCreate)]
    public async Task<IActionResult> Create(CreateEquipmentLogRequest request)
    {
        if (!await db.Equipment.AnyAsync(e => e.Id == request.EquipmentId && e.IsDeleted != true))
        {
            return NotFound(new AuthErrorResponse { Code = "EquipmentNotFound", Message = "Equipment not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            return BadRequest(new AuthErrorResponse { Code = "NicknameRequired", Message = "Nickname is required." });
        }

        if (string.IsNullOrWhiteSpace(request.LogText))
        {
            return BadRequest(new AuthErrorResponse { Code = "LogTextRequired", Message = "Log text is required." });
        }

        if (request.DateTimeUtc == default)
        {
            return BadRequest(new AuthErrorResponse { Code = "DateTimeRequired", Message = "Date/time is required." });
        }

        var entity = new Auth.EquipmentLog
        {
            EquipmentId = request.EquipmentId,
            Nickname = request.Nickname,
            LogText = request.LogText,
            CreatedBy = ResolveCreatedBy(),
            DateTimeUtc = request.DateTimeUtc,
        };
        db.EquipmentLogs.Add(entity);
        await db.SaveChangesAsync();

        return Ok(ToDto(entity));
    }

    [HttpPost(ApiRoutes.EquipmentLogUpdate)]
    public async Task<IActionResult> Update(UpdateEquipmentLogRequest request)
    {
        Auth.EquipmentLog? entity = await db.EquipmentLogs.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "Log not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            return BadRequest(new AuthErrorResponse { Code = "NicknameRequired", Message = "Nickname is required." });
        }

        if (string.IsNullOrWhiteSpace(request.LogText))
        {
            return BadRequest(new AuthErrorResponse { Code = "LogTextRequired", Message = "Log text is required." });
        }

        if (request.DateTimeUtc == default)
        {
            return BadRequest(new AuthErrorResponse { Code = "DateTimeRequired", Message = "Date/time is required." });
        }

        // CreatedBy is deliberately left untouched - it stays the original author even after edits.
        entity.Nickname = request.Nickname;
        entity.LogText = request.LogText;
        entity.DateTimeUtc = request.DateTimeUtc;
        await db.SaveChangesAsync();

        return Ok(ToDto(entity));
    }

    [HttpPost(ApiRoutes.EquipmentLogDelete)]
    public async Task<IActionResult> Delete(DeleteEquipmentLogRequest request)
    {
        Auth.EquipmentLog? entity = await db.EquipmentLogs.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "Log not found." });
        }

        db.EquipmentLogs.Remove(entity);
        await db.SaveChangesAsync();

        return Ok();
    }

    // Resolved from the caller's JWT claims rather than the request body - CreatedBy is an audit
    // field and must not be spoofable by the client. Claim names match what TokenService.
    // CreateAccessToken puts in the token: "displayName" (custom, unmapped) falls back to the
    // standard "email" claim (remapped to ClaimTypes.Email by the default JWT inbound claim map -
    // see AuthController's own ClaimTypes.NameIdentifier lookup for the same convention).
    private string ResolveCreatedBy() =>
        User.FindFirst("displayName")?.Value
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? "Unknown";

    private static EquipmentLogDto ToDto(Auth.EquipmentLog entity) => new()
    {
        Id = entity.Id,
        EquipmentId = entity.EquipmentId,
        Nickname = entity.Nickname,
        LogText = entity.LogText,
        CreatedBy = entity.CreatedBy,
        DateTimeUtc = entity.DateTimeUtc,
    };
}
