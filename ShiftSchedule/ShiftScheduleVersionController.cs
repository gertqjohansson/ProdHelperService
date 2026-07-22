using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProdHelperService.Auth;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.ShiftScheduleVersion;

namespace ProdHelperService;

[ApiController]
[Authorize(Roles = "Administrator")]
public class ShiftScheduleVersionController(ApplicationDbContext db) : ControllerBase
{
    private static readonly int[] AllowedDaysInScedule = [7, 14, 21, 35, 42, 50];

    [HttpPost(ApiRoutes.ShiftScheduleVersionCreate)]
    public async Task<IActionResult> Create(CreateShiftScheduleVersionRequest request)
    {
        if (!await db.Equipment.AnyAsync(e => e.Id == request.EquipmentId && e.IsDeleted != true))
        {
            return NotFound(new AuthErrorResponse { Code = "EquipmentNotFound", Message = "Equipment not found." });
        }

        if (request.StartDate == default)
        {
            return BadRequest(new AuthErrorResponse { Code = "StartDateRequired", Message = "Start date is required." });
        }

        if (request.StartDate.Date <= DateTime.Today)
        {
            return BadRequest(new AuthErrorResponse { Code = "StartDateMustBeFuture", Message = "Start date must be after today." });
        }

        Auth.ShiftScheduleVersion? previous = await db.ShiftScheduleVersions
            .Where(x => x.EquipmentId == request.EquipmentId && !x.IsDeleted)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (previous is not null && request.StartDate.Date < previous.StartDate.Date.AddDays(7))
        {
            return BadRequest(new AuthErrorResponse { Code = "StartDateTooSoonAfterPrevious", Message = "Start date must be at least 7 days after the previous calendar's start date." });
        }

        if (!AllowedDaysInScedule.Contains(request.DaysInScedule))
        {
            return BadRequest(new AuthErrorResponse { Code = "DaysInSceduleInvalid", Message = "Number of days must be one of 7, 14, 21, 35, 42 or 50." });
        }

        var entity = new Auth.ShiftScheduleVersion
        {
            EquipmentId = request.EquipmentId,
            StartDate = request.StartDate,
            DaysInScedule = request.DaysInScedule,
        };
        db.ShiftScheduleVersions.Add(entity);
        await db.SaveChangesAsync();

        return Ok(ToDto(entity));
    }

    [HttpPost(ApiRoutes.ShiftScheduleVersionListEquipmentIdsWithSchedule)]
    public async Task<IActionResult> ListEquipmentIdsWithSchedule()
    {
        List<int> equipmentIds = await db.ShiftScheduleVersions
            .Where(x => !x.IsDeleted)
            .Select(x => x.EquipmentId)
            .Distinct()
            .ToListAsync();

        return Ok(new ListEquipmentIdsWithScheduleResponse { EquipmentIds = equipmentIds });
    }

    [HttpPost(ApiRoutes.ShiftScheduleVersionGetLatestForEquipment)]
    public async Task<IActionResult> GetLatestForEquipment(GetLatestShiftScheduleVersionRequest request)
    {
        Auth.ShiftScheduleVersion? latest = await db.ShiftScheduleVersions
            .Where(x => x.EquipmentId == request.EquipmentId && !x.IsDeleted)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        return Ok(latest is null ? null : ToDto(latest));
    }

    private static ShiftScheduleVersionDto ToDto(Auth.ShiftScheduleVersion entity) => new()
    {
        Id = entity.Id,
        EquipmentId = entity.EquipmentId,
        StartDate = entity.StartDate,
        DaysInScedule = entity.DaysInScedule,
    };
}
