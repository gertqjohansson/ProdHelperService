using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProdHelperService.Auth;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.Equipment;

namespace ProdHelperService;

[ApiController]
[Authorize(Roles = "Administrator")]
public class EquipmentController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost(ApiRoutes.EquipmentList)]
    public async Task<IActionResult> List(EquipmentListRequest request)
    {
        string fallbackLanguage = await db.Languages
            .Where(l => l.IsFallback == true)
            .Select(l => l.IsoCode)
            .FirstOrDefaultAsync() ?? "en";
        string language = string.IsNullOrWhiteSpace(request.LanguageIsoCode) ? fallbackLanguage : request.LanguageIsoCode;

        List<Equipment> equipment = await db.Equipment.Where(e => e.IsDeleted != true).ToListAsync();
        List<int> ids = equipment.Select(e => e.Id).ToList();
        List<EquipmentTranslation> translations = await db.EquipmentTranslations
            .Where(t => ids.Contains(t.EquipmentId) && (t.LanguageIsoCode == language || t.LanguageIsoCode == fallbackLanguage))
            .ToListAsync();

        List<int> categoryIds = equipment.Where(e => e.EquipmentCategoryId != null).Select(e => e.EquipmentCategoryId!.Value).Distinct().ToList();
        List<EquipmentCategoryTranslation> categoryTranslations = await db.EquipmentCategoryTranslations
            .Where(t => categoryIds.Contains(t.EquipmentCategoryId) && (t.LanguageIsoCode == language || t.LanguageIsoCode == fallbackLanguage))
            .ToListAsync();

        List<EquipmentDto> items = equipment.Select(e =>
        {
            string name = translations.FirstOrDefault(t => t.EquipmentId == e.Id && t.LanguageIsoCode == language)?.Value
                ?? translations.FirstOrDefault(t => t.EquipmentId == e.Id && t.LanguageIsoCode == fallbackLanguage)?.Value
                ?? string.Empty;

            string? categoryName = e.EquipmentCategoryId is null ? null :
                categoryTranslations.FirstOrDefault(t => t.EquipmentCategoryId == e.EquipmentCategoryId && t.LanguageIsoCode == language)?.Value
                ?? categoryTranslations.FirstOrDefault(t => t.EquipmentCategoryId == e.EquipmentCategoryId && t.LanguageIsoCode == fallbackLanguage)?.Value;

            return new EquipmentDto
            {
                Id = e.Id,
                ParentId = e.ParentId,
                Name = name,
                ExternalCode = e.ExternalCode,
                IsOee = e.IsOee,
                IsPlannable = e.IsPlannable,
                ColorCode = e.ColorCode,
                EquipmentCategoryId = e.EquipmentCategoryId,
                EquipmentCategoryName = categoryName,
                UseEconomy = e.UseEconomy,
                DateOfPurchase = e.DateOfPurchase,
                Price = e.Price,
                DepreciationPeriod = e.DepreciationPeriod,
                UseNotification = e.UseNotification,
                NotificationDate = e.NotificationDate,
                Notification = e.Notification,
            };
        }).ToList();

        return Ok(new EquipmentListResponse { Items = items });
    }

    [HttpPost(ApiRoutes.EquipmentCreate)]
    public async Task<IActionResult> Create(CreateEquipmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new AuthErrorResponse { Code = "NameRequired", Message = "Name is required." });
        }

        if (request.ParentId is not null && !await db.Equipment.AnyAsync(e => e.Id == request.ParentId && e.IsDeleted != true))
        {
            return BadRequest(new AuthErrorResponse { Code = "ParentNotFound", Message = "Parent equipment does not exist." });
        }

        if (request.EquipmentCategoryId <= 0)
        {
            return BadRequest(new AuthErrorResponse { Code = "CategoryRequired", Message = "Select a category." });
        }

        if (!await db.EquipmentCategories.AnyAsync(c => c.Id == request.EquipmentCategoryId))
        {
            return BadRequest(new AuthErrorResponse { Code = "CategoryNotFound", Message = "The selected category does not exist." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var entity = new Equipment
        {
            ParentId = request.ParentId,
            ExternalCode = request.ExternalCode,
            IsOee = request.IsOee,
            IsPlannable = request.IsPlannable,
            ColorCode = request.ColorCode,
            EquipmentCategoryId = request.EquipmentCategoryId,
            UseEconomy = request.UseEconomy,
            DateOfPurchase = request.UseEconomy == true ? request.DateOfPurchase : null,
            Price = request.UseEconomy == true ? request.Price : null,
            DepreciationPeriod = request.UseEconomy == true ? request.DepreciationPeriod : null,
            UseNotification = request.UseNotification,
            NotificationDate = request.UseNotification == true ? request.NotificationDate : null,
            Notification = request.UseNotification == true ? request.Notification : null,
        };
        db.Equipment.Add(entity);
        await db.SaveChangesAsync(); // Save first so entity.Id is populated for the translations below.

        // Only the user's language + the fallback language get a translation row - not every
        // language in the system (see Client/SKILL.md's translation-table storage rule).
        string fallbackLanguage = await db.Languages
            .Where(l => l.IsFallback == true)
            .Select(l => l.IsoCode)
            .FirstOrDefaultAsync() ?? "en";
        string language = string.IsNullOrWhiteSpace(request.LanguageIsoCode) ? fallbackLanguage : request.LanguageIsoCode;

        db.EquipmentTranslations.Add(new EquipmentTranslation { EquipmentId = entity.Id, LanguageIsoCode = language, Value = request.Name });
        if (language != fallbackLanguage)
        {
            db.EquipmentTranslations.Add(new EquipmentTranslation { EquipmentId = entity.Id, LanguageIsoCode = fallbackLanguage, Value = request.Name });
        }
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(new EquipmentDto
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Name = request.Name,
            ExternalCode = entity.ExternalCode,
            IsOee = entity.IsOee,
            IsPlannable = entity.IsPlannable,
            ColorCode = entity.ColorCode,
            EquipmentCategoryId = entity.EquipmentCategoryId,
            UseEconomy = entity.UseEconomy,
            DateOfPurchase = entity.DateOfPurchase,
            Price = entity.Price,
            DepreciationPeriod = entity.DepreciationPeriod,
            UseNotification = entity.UseNotification,
            NotificationDate = entity.NotificationDate,
            Notification = entity.Notification,
        });
    }

    [HttpPost(ApiRoutes.EquipmentUpdate)]
    public async Task<IActionResult> Update(UpdateEquipmentRequest request)
    {
        Equipment? entity = await db.Equipment.FirstOrDefaultAsync(e => e.Id == request.Id && e.IsDeleted != true);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "Equipment not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new AuthErrorResponse { Code = "NameRequired", Message = "Name is required." });
        }

        if (request.ParentId is not null && !await db.Equipment.AnyAsync(e => e.Id == request.ParentId && e.IsDeleted != true))
        {
            return BadRequest(new AuthErrorResponse { Code = "ParentNotFound", Message = "Parent equipment does not exist." });
        }

        if (request.EquipmentCategoryId <= 0)
        {
            return BadRequest(new AuthErrorResponse { Code = "CategoryRequired", Message = "Select a category." });
        }

        if (!await db.EquipmentCategories.AnyAsync(c => c.Id == request.EquipmentCategoryId))
        {
            return BadRequest(new AuthErrorResponse { Code = "CategoryNotFound", Message = "The selected category does not exist." });
        }

        entity.ParentId = request.ParentId;
        entity.ExternalCode = request.ExternalCode;
        entity.IsOee = request.IsOee;
        entity.IsPlannable = request.IsPlannable;
        entity.ColorCode = request.ColorCode;
        entity.EquipmentCategoryId = request.EquipmentCategoryId;
        entity.UseEconomy = request.UseEconomy;
        entity.DateOfPurchase = request.UseEconomy == true ? request.DateOfPurchase : null;
        entity.Price = request.UseEconomy == true ? request.Price : null;
        entity.DepreciationPeriod = request.UseEconomy == true ? request.DepreciationPeriod : null;
        entity.UseNotification = request.UseNotification;
        entity.NotificationDate = request.UseNotification == true ? request.NotificationDate : null;
        entity.Notification = request.UseNotification == true ? request.Notification : null;

        // Editing only touches the user's own language row - every other language, including the
        // fallback, is left as-is (see Client/SKILL.md's translation-table storage rule).
        string fallbackLanguage = await db.Languages
            .Where(l => l.IsFallback == true)
            .Select(l => l.IsoCode)
            .FirstOrDefaultAsync() ?? "en";
        string language = string.IsNullOrWhiteSpace(request.LanguageIsoCode) ? fallbackLanguage : request.LanguageIsoCode;

        EquipmentTranslation? translation = await db.EquipmentTranslations
            .FirstOrDefaultAsync(t => t.EquipmentId == entity.Id && t.LanguageIsoCode == language);
        if (translation is null)
        {
            db.EquipmentTranslations.Add(new EquipmentTranslation { EquipmentId = entity.Id, LanguageIsoCode = language, Value = request.Name });
        }
        else
        {
            translation.Value = request.Name;
        }

        await db.SaveChangesAsync();

        return Ok(new EquipmentDto
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
            Name = request.Name,
            ExternalCode = entity.ExternalCode,
            IsOee = entity.IsOee,
            IsPlannable = entity.IsPlannable,
            ColorCode = entity.ColorCode,
            EquipmentCategoryId = entity.EquipmentCategoryId,
            UseEconomy = entity.UseEconomy,
            DateOfPurchase = entity.DateOfPurchase,
            Price = entity.Price,
            DepreciationPeriod = entity.DepreciationPeriod,
            UseNotification = entity.UseNotification,
            NotificationDate = entity.NotificationDate,
            Notification = entity.Notification,
        });
    }

    [HttpPost(ApiRoutes.EquipmentDelete)]
    public async Task<IActionResult> Delete(DeleteEquipmentRequest request)
    {
        Equipment? entity = await db.Equipment.FirstOrDefaultAsync(e => e.Id == request.Id && e.IsDeleted != true);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "Equipment not found." });
        }

        bool hasChildren = await db.Equipment.AnyAsync(e => e.ParentId == entity.Id && e.IsDeleted != true);
        if (hasChildren)
        {
            return BadRequest(new AuthErrorResponse { Code = "HasChildren", Message = "Cannot delete equipment that has child nodes." });
        }

        entity.IsDeleted = true;
        await db.SaveChangesAsync();

        return Ok();
    }
}
