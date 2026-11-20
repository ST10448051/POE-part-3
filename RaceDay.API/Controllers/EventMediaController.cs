
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;
using System.Security.Claims;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/media")]
public class EventMediaController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly RaceDayDbContext _context;
    private readonly BlobStorageService _storage;
    private readonly ILogger<EventMediaController> _logger;

    public EventMediaController(
        RaceDayDbContext context,
        BlobStorageService storage,
        ILogger<EventMediaController> logger)
    {
        _context = context;
        _storage = storage;
        _logger = logger;
    }

    private int? CurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id)
            ? id
            : null;
    }

    private async Task<bool> OwnsEvent(
        int eventId, int organiserId)
    {
        return await _context.Events.AnyAsync(e =>
            e.EventID == eventId &&
            e.OrganiserID == organiserId);
    }

    // GET: api/events/5/media
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventMedia(int eventId)
    {
        if (!await _context.Events.AnyAsync(
            e => e.EventID == eventId))
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        var media = await _context.EventMedia
            .AsNoTracking()
            .Where(m => m.EventID == eventId)
            .OrderByDescending(m => m.UploadedAt)
            .Select(m => new
            {
                m.EventMediaID,
                m.EventID,
                m.FileName,
                m.ContentType,
                m.FileSize,
                m.UploadedAt,
                ImageUrl =
                    $"/api/events/{eventId}/media/{m.EventMediaID}"
            })
            .ToListAsync();

        return Ok(media);
    }

    // GET: api/events/5/media/10
    [HttpGet("{mediaId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMedia(
        int eventId, int mediaId)
    {
        var media = await _context.EventMedia
            .AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.EventMediaID == mediaId &&
                m.EventID == eventId);

        if (media == null)
            return NotFound();

        try
        {
            var stream = await _storage.DownloadAsync(
                media.BlobName);

            return File(stream, media.ContentType);
        }
        catch (RequestFailedException ex)
            when (ex.Status == 404)
        {
            _logger.LogWarning(
                "Blob missing for media {MediaID}", mediaId);

            return NotFound(new
            {
                message = "Media file not found in storage."
            });
        }
    }

    // POST: api/events/5/media
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(
        int eventId,
        [FromForm] IFormFile file)
    {
        var organiserId = CurrentUserId();

        if (organiserId == null)
            return Unauthorized();

        if (!await OwnsEvent(eventId, organiserId.Value))
            return NotFound(new
            {
                message = "Event not found or access denied."
            });

        if (file == null || file.Length == 0)
            return BadRequest(new
            {
                message = "Please select an image."
            });

        if (file.Length > MaxFileSize)
            return BadRequest(new
            {
                message = "Maximum image size is 5 MB."
            });

        await using var buffer = new MemoryStream();

        await file.CopyToAsync(buffer);

        if (buffer.Length > MaxFileSize)
            return BadRequest(new
            {
                message = "Maximum image size is 5 MB."
            });

        var bytes = buffer.ToArray();

        var detectedType = DetectImageType(bytes);

        if (detectedType == null)
            return BadRequest(new
            {
                message = "Only JPEG, PNG and WebP images are supported."
            });

        var extension = detectedType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ""
        };

        // Use the detected format, not the uploaded extension.
        var originalName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(originalName))
            originalName = "image" + extension;

        if (originalName.Length > 255)
            originalName = originalName[..255];

        buffer.Position = 0;

        string? blobName = null;

        try
        {
            blobName = await _storage.UploadAsync(
                buffer,
                "image" + extension,
                detectedType);

            var media = new EventMedia
            {
                EventID = eventId,
                BlobName = blobName,
                FileName = originalName,
                ContentType = detectedType,
                FileSize = buffer.Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.EventMedia.Add(media);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMedia),
                new
                {
                    eventId,
                    mediaId = media.EventMediaID
                },
                new
                {
                    media.EventMediaID,
                    media.EventID,
                    media.FileName,
                    media.ContentType,
                    media.FileSize,
                    media.UploadedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload media for event {EventID}",
                eventId);

            // Remove the blob if SQL Server saving failed.
            if (blobName != null)
            {
                try
                {
                    await _storage.DeleteAsync(blobName);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(
                        cleanupEx,
                        "Failed to clean up blob {BlobName}",
                        blobName);
                }
            }

            return StatusCode(500, new
            {
                message = "Unable to upload image."
            });
        }
    }

    // DELETE: api/events/5/media/10
    [HttpDelete("{mediaId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteMedia(
        int eventId, int mediaId)
    {
        var organiserId = CurrentUserId();

        if (organiserId == null)
            return Unauthorized();

        if (!await OwnsEvent(eventId, organiserId.Value))
            return NotFound();

        var media = await _context.EventMedia
            .FirstOrDefaultAsync(m =>
                m.EventMediaID == mediaId &&
                m.EventID == eventId);

        if (media == null)
            return NotFound();

        try
        {
            // Delete the blob first. If SQL deletion fails,
            // a later retry can still remove the database record.
            await _storage.DeleteAsync(media.BlobName);

            _context.EventMedia.Remove(media);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete media {MediaID}",
                mediaId);

            return StatusCode(500, new
            {
                message = "Unable to delete image."
            });
        }
    }

    private static string? DetectImageType(byte[] data)
    {
        if (data.Length >= 3 &&
            data[0] == 0xFF &&
            data[1] == 0xD8 &&
            data[2] == 0xFF)
        {
            return "image/jpeg";
        }

        byte[] pngSignature =
        {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        };

        if (data.Length >= 8 &&
            data.Take(8).SequenceEqual(pngSignature))
        {
            return "image/png";
        }

        if (data.Length >= 12 &&
            System.Text.Encoding.ASCII.GetString(
                data, 0, 4) == "RIFF" &&
            System.Text.Encoding.ASCII.GetString(
                data, 8, 4) == "WEBP")
        {
            return "image/webp";
        }

        return null;
    }
}
