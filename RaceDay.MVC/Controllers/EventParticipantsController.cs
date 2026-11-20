
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers;

[Authorize(Roles = "Organiser")]
public class EventParticipantsController : Controller
{
    private readonly AuthenticatedApiService _authenticatedApi;
    private readonly ILogger<EventParticipantsController> _logger;

    public EventParticipantsController(
        AuthenticatedApiService authenticatedApi,
        ILogger<EventParticipantsController> logger)
    {
        _authenticatedApi = authenticatedApi;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        if (eventId <= 0)
            return NotFound();

        try
        {
            using var response = await _authenticatedApi
                .GetAsync($"api/events/{eventId}/participants");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] =
                    "Your API session has expired. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to retrieve participants for this event.";

                return RedirectToAction("Index", "Organiser");
            }

            var model = await response.Content
                .ReadFromJsonAsync<EventParticipantsViewModel>(
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (model == null)
            {
                TempData["ErrorMessage"] =
                    "The API returned an empty response.";

                return RedirectToAction("Index", "Organiser");
            }

            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] =
                "Your API session has expired.";

            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to load participants for event {EventID}",
                eventId);

            TempData["ErrorMessage"] =
                "Unable to connect to the RaceDay API.";

            return RedirectToAction("Index", "Organiser");
        }
    }
}
