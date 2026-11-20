using RaceDay.MVC.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers;

public class EventMediaController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EventMediaController(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // POST: /EventMedia/Upload
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        int eventId,
        IFormFile? file)
    {
        if (eventId <= 0)
            return BadRequest();

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select an image.";
            return RedirectToAction(
                nameof(Index), new { eventId });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "Maximum image size is 5 MB.";
            return RedirectToAction(
                nameof(Index), new { eventId });
        }

        try
        {
            using var response = await _api.PostFileAsync(
                $"api/events/{eventId}/media", file);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Image uploaded successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Image upload failed. Please check the file " +
                    "and confirm you own this event.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException)
        {
            TempData["Error"] =
                "Unable to contact the RaceDay API.";
        }

        return RedirectToAction(
            nameof(Index), new { eventId });
    }


    // POST: /EventMedia/Delete
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int eventId,
        int mediaId)
    {
        if (eventId <= 0 || mediaId <= 0)
            return BadRequest();

        try
        {
            using var response = await _api.DeleteAsync(
                $"api/events/{eventId}/media/{mediaId}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Image deleted successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Unable to delete image. " +
                    "Please confirm you own this event.";
            }
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException)
        {
            TempData["Error"] =
                "Unable to contact the RaceDay API.";
        }

        return RedirectToAction(
            nameof(Index), new { eventId });
    }


    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(int eventId)
    {
        if (eventId <= 0)
            return NotFound();

        var client = _httpClientFactory.CreateClient("RaceDayAPI");

        using var eventResponse = await client.GetAsync(
            $"api/events/{eventId}");

        if (!eventResponse.IsSuccessStatusCode)
            return NotFound();

        var raceEvent = await eventResponse.Content
            .ReadFromJsonAsync<EventViewModel>();

        if (raceEvent == null)
            return NotFound();

        using var response = await client.GetAsync(
            $"api/events/{eventId}/media");

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var images = await response.Content
            .ReadFromJsonAsync<List<EventMediaViewModel>>()
            ?? new List<EventMediaViewModel>();

        var model = new EventMediaGalleryViewModel
        {
            EventID = eventId,
            EventName = raceEvent.EventName,
            Images = images
        };

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Image(
        int eventId, int mediaId)
    {
        if (eventId <= 0 || mediaId <= 0)
            return NotFound();

        var client = _httpClientFactory.CreateClient("RaceDayAPI");

        using var response = await client.GetAsync(
            $"api/events/{eventId}/media/{mediaId}",
            HttpCompletionOption.ResponseHeadersRead);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var contentType = response.Content.Headers.ContentType?
            .ToString() ?? "application/octet-stream";

        var bytes = await response.Content.ReadAsByteArrayAsync();

        return File(bytes, contentType);
    }
    private readonly AuthenticatedApiService _api;

    public EventMediaController(
        IHttpClientFactory httpClientFactory,
        AuthenticatedApiService api)
    {
        _httpClientFactory = httpClientFactory;
        _api = api;
    }

}
