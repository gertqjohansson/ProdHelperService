using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProdHelperService.Auth;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.EquipmentCategory;

namespace ProdHelperService;

[ApiController]
[Authorize(Roles = "Administrator")]
public class EquipmentCategoryController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost(ApiRoutes.EquipmentCategoryList)]
    public async Task<IActionResult> List(EquipmentCategoryListRequest request)
    {
        string fallbackLanguage = await db.Languages
            .Where(l => l.IsFallback == true)
            .Select(l => l.IsoCode)
            .FirstOrDefaultAsync() ?? "en";
        string language = string.IsNullOrWhiteSpace(request.LanguageIsoCode) ? fallbackLanguage : request.LanguageIsoCode;

        List<EquipmentCategory> categories = await db.EquipmentCategories.ToListAsync();
        List<int> ids = categories.Select(c => c.Id).ToList();
        List<EquipmentCategoryTranslation> translations = await db.EquipmentCategoryTranslations
            .Where(t => ids.Contains(t.EquipmentCategoryId) && (t.LanguageIsoCode == language || t.LanguageIsoCode == fallbackLanguage))
            .ToListAsync();

        List<EquipmentCategoryDto> items = categories.Select(c =>
        {
            string name = translations.FirstOrDefault(t => t.EquipmentCategoryId == c.Id && t.LanguageIsoCode == language)?.Value
                ?? translations.FirstOrDefault(t => t.EquipmentCategoryId == c.Id && t.LanguageIsoCode == fallbackLanguage)?.Value
                ?? string.Empty;

            return new EquipmentCategoryDto { Id = c.Id, Name = name, ColorCode = c.ColorCode };
        }).ToList();

        return Ok(new EquipmentCategoryListResponse { Items = items });
    }

    [HttpPost(ApiRoutes.EquipmentCategoryCreate)]
    public async Task<IActionResult> Create(CreateEquipmentCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new AuthErrorResponse { Code = "NameRequired", Message = "Name is required." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var entity = new EquipmentCategory { ColorCode = request.ColorCode };
        db.EquipmentCategories.Add(entity);
        await db.SaveChangesAsync(); // Save first so entity.Id is populated for the translations below.

        // Only the user's language + the fallback language get a translation row - not every
        // language in the system (see Client/SKILL.md's translation-table storage rule).
        string fallbackLanguage = await db.Languages
            .Where(l => l.IsFallback == true)
            .Select(l => l.IsoCode)
            .FirstOrDefaultAsync() ?? "en";
        string language = string.IsNullOrWhiteSpace(request.LanguageIsoCode) ? fallbackLanguage : request.LanguageIsoCode;

        db.EquipmentCategoryTranslations.Add(new EquipmentCategoryTranslation { EquipmentCategoryId = entity.Id, LanguageIsoCode = language, Value = request.Name });
        if (language != fallbackLanguage)
        {
            db.EquipmentCategoryTranslations.Add(new EquipmentCategoryTranslation { EquipmentCategoryId = entity.Id, LanguageIsoCode = fallbackLanguage, Value = request.Name });
        }
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(new EquipmentCategoryDto { Id = entity.Id, Name = request.Name, ColorCode = entity.ColorCode });
    }

    [HttpPost(ApiRoutes.EquipmentCategoryUpdate)]
    public async Task<IActionResult> Update(UpdateEquipmentCategoryRequest request)
    {
        EquipmentCategory? entity = await db.EquipmentCategories.FirstOrDefaultAsync(c => c.Id == request.Id);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "Equipment category not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new AuthErrorResponse { Code = "NameRequired", Message = "Name is required." });
        }

        entity.ColorCode = request.ColorCode;

        // Editing only touches the user's own language row - every other language, including the
        // fallback, is left as-is (see Client/SKILL.md's translation-table storage rule).
        string fallbackLanguage = await db.Languages
            .Where(l => l.IsFallback == true)
            .Select(l => l.IsoCode)
            .FirstOrDefaultAsync() ?? "en";
        string language = string.IsNullOrWhiteSpace(request.LanguageIsoCode) ? fallbackLanguage : request.LanguageIsoCode;

        EquipmentCategoryTranslation? translation = await db.EquipmentCategoryTranslations
            .FirstOrDefaultAsync(t => t.EquipmentCategoryId == entity.Id && t.LanguageIsoCode == language);
        if (translation is null)
        {
            db.EquipmentCategoryTranslations.Add(new EquipmentCategoryTranslation { EquipmentCategoryId = entity.Id, LanguageIsoCode = language, Value = request.Name });
        }
        else
        {
            translation.Value = request.Name;
        }

        await db.SaveChangesAsync();

        return Ok(new EquipmentCategoryDto { Id = entity.Id, Name = request.Name, ColorCode = entity.ColorCode });
    }

    [HttpPost(ApiRoutes.EquipmentCategoryDelete)]
    public async Task<IActionResult> Delete(DeleteEquipmentCategoryRequest request)
    {
        EquipmentCategory? entity = await db.EquipmentCategories.FirstOrDefaultAsync(c => c.Id == request.Id);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "Equipment category not found." });
        }

        bool inUse = await db.Equipment.AnyAsync(e => e.EquipmentCategoryId == entity.Id && e.IsDeleted != true);
        if (inUse)
        {
            return BadRequest(new AuthErrorResponse { Code = "CategoryInUse", Message = "This category is still assigned to equipment and cannot be deleted." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        List<EquipmentCategoryTranslation> translations = await db.EquipmentCategoryTranslations
            .Where(t => t.EquipmentCategoryId == entity.Id)
            .ToListAsync();
        db.EquipmentCategoryTranslations.RemoveRange(translations);
        db.EquipmentCategories.Remove(entity);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok();
    }
}
