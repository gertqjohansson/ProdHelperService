using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProdHelperService.ActionLogging;
using ProdHelperService.Auth;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.EquipmentUpload;
using ProdHelperService.Storage;

namespace ProdHelperService;

// File bytes travel as base64 inside a JSON body rather than multipart/form-data because every
// client request is proxied through an Azure Relay Hybrid Connection (see RelayListener.cs,
// ProxyToLocalApi) built around simple, non-preflighted JSON requests. Multipart/form-data would
// need new CORS/relay plumbing that doesn't exist today.
[ApiController]
[Authorize(Roles = "Administrator")]
public class EquipmentUploadController(ApplicationDbContext db, IFileStorageService storage, IOptions<StorageOptions> storageOptions, IActionLogService actionLogService) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    // Inferred from the file extension rather than trusting a client-supplied Content-Type -
    // there's no IFormFile here to derive it from automatically (see the base64/JSON note above).
    private static string ResolveContentType(string fileName) =>
        ContentTypeProvider.TryGetContentType(fileName, out string? contentType)
            ? contentType
            : "application/octet-stream";

    [HttpPost(ApiRoutes.EquipmentUploadList)]
    public async Task<IActionResult> List(EquipmentUploadListRequest request)
    {
        List<Auth.EquipmentUpload> uploads = await db.EquipmentUploads
            .Where(x => x.EquipmentId == request.EquipmentId)
            .OrderBy(x => x.Nickname)
            .ToListAsync();

        List<EquipmentUploadDto> items = uploads.Select(x => new EquipmentUploadDto
        {
            Id = x.Id,
            EquipmentId = x.EquipmentId,
            Nickname = x.Nickname,
            FileName = x.FileName,
        }).ToList();

        return Ok(new EquipmentUploadListResponse { Items = items });
    }

    [HttpPost(ApiRoutes.EquipmentUploadUpload)]
    public async Task<IActionResult> Upload(UploadEquipmentFileRequest request)
    {
        if (!storage.IsConfigured)
        {
            return BadRequest(new AuthErrorResponse { Code = "StorageNotConfigured", Message = "File storage is not configured. Set Storage:UploadPath (e.g. via the AdminApp tool) and restart the service." });
        }

        if (!await db.Equipment.AnyAsync(e => e.Id == request.EquipmentId && e.IsDeleted != true))
        {
            return NotFound(new AuthErrorResponse { Code = "EquipmentNotFound", Message = "Equipment not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            return BadRequest(new AuthErrorResponse { Code = "NicknameRequired", Message = "Nickname is required." });
        }

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrEmpty(request.ContentBase64))
        {
            return BadRequest(new AuthErrorResponse { Code = "FileRequired", Message = "A file is required." });
        }

        string fileName;
        try
        {
            fileName = storage.SanitizeFileName(request.FileName);
        }
        catch (ArgumentException)
        {
            return BadRequest(new AuthErrorResponse { Code = "FileNameInvalid", Message = "The file name is invalid." });
        }

        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.ContentBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new AuthErrorResponse { Code = "FileRequired", Message = "The file content is invalid." });
        }

        if (content.Length == 0)
        {
            return BadRequest(new AuthErrorResponse { Code = "FileRequired", Message = "The file is empty." });
        }

        if (content.Length > storageOptions.Value.MaxFileSizeBytes)
        {
            return BadRequest(new AuthErrorResponse { Code = "FileTooLarge", Message = "The file is too large." });
        }

        if (request.UploadedAtUtc == default)
        {
            return BadRequest(new AuthErrorResponse { Code = "DateTimeRequired", Message = "Date/time is required." });
        }

        if (string.IsNullOrWhiteSpace(request.MadeByUser))
        {
            return BadRequest(new AuthErrorResponse { Code = "MadeByUserRequired", Message = "The user is required." });
        }

        Auth.EquipmentUpload? existing = await db.EquipmentUploads
            .FirstOrDefaultAsync(x => x.EquipmentId == request.EquipmentId && x.FileName == fileName);

        if (existing is not null && !request.Overwrite)
        {
            return BadRequest(new AuthErrorResponse { Code = "FileAlreadyExists", Message = "A file with this name already exists for this equipment." });
        }

        // Write the file to disk first, then commit the DB row - if the disk write throws, no DB
        // row is written; if the DB write throws, an orphaned file on disk is the acceptable
        // failure mode (harmless - it's just overwritten or ignored on the next attempt), not a
        // DB row pointing at a missing file.
        await storage.SaveFileAsync(request.EquipmentId, fileName, content);

        await using var transaction = await db.Database.BeginTransactionAsync();

        if (existing is not null)
        {
            string oldValuesJson = JsonSerializer.Serialize(existing);
            existing.Nickname = request.Nickname;
            existing.ContentType = ResolveContentType(fileName);
            existing.FileSizeBytes = content.Length;
            existing.UploadedAtUtc = request.UploadedAtUtc;
            actionLogService.Record("Update", "EquipmentUpload", request.MadeByUser, request.UploadedAtUtc, oldValuesJson, JsonSerializer.Serialize(existing));
        }
        else
        {
            existing = new Auth.EquipmentUpload
            {
                EquipmentId = request.EquipmentId,
                Nickname = request.Nickname,
                FileName = fileName,
                ContentType = ResolveContentType(fileName),
                FileSizeBytes = content.Length,
                UploadedAtUtc = request.UploadedAtUtc,
            };
            db.EquipmentUploads.Add(existing);
            actionLogService.Record("New", "EquipmentUpload", request.MadeByUser, request.UploadedAtUtc, null, JsonSerializer.Serialize(existing));
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new EquipmentUploadDto
        {
            Id = existing.Id,
            EquipmentId = existing.EquipmentId,
            Nickname = existing.Nickname,
            FileName = existing.FileName,
        });
    }

    [HttpPost(ApiRoutes.EquipmentUploadDownload)]
    public async Task<IActionResult> Download(DownloadEquipmentFileRequest request)
    {
        if (!storage.IsConfigured)
        {
            return BadRequest(new AuthErrorResponse { Code = "StorageNotConfigured", Message = "File storage is not configured. Set Storage:UploadPath (e.g. via the AdminApp tool) and restart the service." });
        }

        Auth.EquipmentUpload? entity = await db.EquipmentUploads.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "File not found." });
        }

        if (!storage.FileExists(entity.EquipmentId, entity.FileName))
        {
            return NotFound(new AuthErrorResponse { Code = "FileMissingOnDisk", Message = "The file is missing on disk." });
        }

        byte[] content = await storage.ReadFileAsync(entity.EquipmentId, entity.FileName);

        return Ok(new DownloadEquipmentFileResponse
        {
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            ContentBase64 = Convert.ToBase64String(content),
        });
    }

    [HttpPost(ApiRoutes.EquipmentUploadDelete)]
    public async Task<IActionResult> Delete(DeleteEquipmentFileRequest request)
    {
        Auth.EquipmentUpload? entity = await db.EquipmentUploads.FirstOrDefaultAsync(x => x.Id == request.Id);
        if (entity is null)
        {
            return NotFound(new AuthErrorResponse { Code = "NotFound", Message = "File not found." });
        }

        if (request.ActionTimeUtc == default)
        {
            return BadRequest(new AuthErrorResponse { Code = "DateTimeRequired", Message = "Date/time is required." });
        }

        if (string.IsNullOrWhiteSpace(request.MadeByUser))
        {
            return BadRequest(new AuthErrorResponse { Code = "MadeByUserRequired", Message = "The user is required." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        string oldValuesJson = JsonSerializer.Serialize(entity);
        db.EquipmentUploads.Remove(entity);
        actionLogService.Record("Delete", "EquipmentUpload", request.MadeByUser, request.ActionTimeUtc, oldValuesJson, "{}");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        // The DB row is the source of truth for whether this upload "exists" - if storage isn't
        // configured (or the file is already gone), that's not a reason to fail deleting the row.
        if (storage.IsConfigured)
        {
            storage.DeleteFile(entity.EquipmentId, entity.FileName);
        }

        return Ok();
    }
}
