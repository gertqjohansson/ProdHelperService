using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProdHelperService.ActionLogging;
using ProdHelperService.Auth;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.EquipmentLink;

namespace ProdHelperService;

[ApiController]
[Authorize(Roles = "Administrator")]
public class EquipmentLinkController(ApplicationDbContext db, IActionLogService actionLogService) : ControllerBase
{
    [HttpPost(ApiRoutes.EquipmentLinkList)]
    public async Task<IActionResult> List(EquipmentLinkListRequest request)
    {
        List<Auth.EquipmentLink> links = await db.EquipmentLinks
            .Where(x => x.EquipmentId == request.EquipmentId)
            .OrderBy(x => x.Nickname)
            .ToListAsync();

        List<EquipmentLinkDto> items = links.Select(x => new EquipmentLinkDto
        {
            Id = x.Id,
            EquipmentId = x.EquipmentId,
            Nickname = x.Nickname,
            Path = x.Path,
            IsDocument = x.IsDocument,
        }).ToList();

        return Ok(new EquipmentLinkListResponse { Items = items });
    }

    [HttpPost(ApiRoutes.EquipmentLinkCreate)]
    public async Task<IActionResult> Create(CreateEquipmentLinkRequest request)
    {
        if (!await db.Equipment.AnyAsync(e => e.Id == request.EquipmentId && e.IsDeleted != true))
        {
            return NotFound(new AuthErrorResponse { Code = "EquipmentNotFound", Message = "Equipment not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            return BadRequest(new AuthErrorResponse { Code = "NicknameRequired", Message = "Nickname is required." });
        }

        bool isValidUrl = Uri.TryCreate(request.Path, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (!isValidUrl)
        {
            return BadRequest(new AuthErrorResponse { Code = "PathInvalid", Message = "Enter a valid http(s) URL." });
        }

        if (request.ActionTimeUtc == default)
        {
            return BadRequest(new AuthErrorResponse { Code = "DateTimeRequired", Message = "Date/time is required." });
        }

        if (string.IsNullOrWhiteSpace(request.MadeByUser))
        {
            return BadRequest(new AuthErrorResponse { Code = "MadeByUserRequired", Message = "The user is required." });
        }

        var entity = new Auth.EquipmentLink
        {
            EquipmentId = request.EquipmentId,
            Nickname = request.Nickname,
            Path = request.Path,
            IsDocument = request.IsDocument,
        };
        db.EquipmentLinks.Add(entity);
        actionLogService.Record("New", "EquipmentLink", request.MadeByUser, request.ActionTimeUtc, null, JsonSerializer.Serialize(entity));
        await db.SaveChangesAsync();

        return Ok(new EquipmentLinkDto
        {
            Id = entity.Id,
            EquipmentId = entity.EquipmentId,
            Nickname = entity.Nickname,
            Path = entity.Path,
            IsDocument = entity.IsDocument,
        });
    }

    [HttpPost(ApiRoutes.EquipmentLinkDelete)]
    public async Task<IActionResult> Delete(DeleteEquipmentLinkRequest request)
    {
        Auth.EquipmentLink? entity = await db.EquipmentLinks.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "Link not found." });
        }

        if (request.ActionTimeUtc == default)
        {
            return BadRequest(new AuthErrorResponse { Code = "DateTimeRequired", Message = "Date/time is required." });
        }

        if (string.IsNullOrWhiteSpace(request.MadeByUser))
        {
            return BadRequest(new AuthErrorResponse { Code = "MadeByUserRequired", Message = "The user is required." });
        }

        string oldValuesJson = JsonSerializer.Serialize(entity);
        db.EquipmentLinks.Remove(entity);
        actionLogService.Record("Delete", "EquipmentLink", request.MadeByUser, request.ActionTimeUtc, oldValuesJson, "{}");
        await db.SaveChangesAsync();

        return Ok();
    }
}
