
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers;

[Authorize(Roles = "Organiser")]
[Route("Events/{eventId:int}/Categories")]
public class EventCategoriesController : Controller
{
    private readonly RaceDayApiService _api;
    private readonly AuthenticatedApiService _authenticatedApi;
    private readonly ILogger<EventCategoriesController> _logger;

    public EventCategoriesController(
        RaceDayApiService api,
        AuthenticatedApiService authenticatedApi,
        ILogger<EventCategoriesController> logger)
    {
        _api = api;
        _authenticatedApi = authenticatedApi;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        if (eventId <= 0)
            return NotFound();

        try
        {
            var raceEvent = await _api
                .GetAsync<EventViewModel>(
                    $"api/events/{eventId}");

            if (raceEvent == null)
                return NotFound();

            var assigned = await _api
                .GetListAsync<EventCategoryViewModel>(
                    $"api/events/{eventId}/categories");

            var allCategories = await _api
                .GetListAsync<CategoryViewModel>(
                    "api/categories");

            var assignedIds = assigned
                .Select(c => c.CategoryID)
                .ToHashSet();

            var model = new ManageEventCategoriesViewModel
            {
                EventID = eventId,
                EventName = raceEvent.EventName,
                AssignedCategories = assigned,
                NewCategory = new AddEventCategoryViewModel
                {
                    EventID = eventId,
                    AvailableCategories = allCategories
                        .Where(c => !assignedIds.Contains(c.CategoryID))
                        .Select(c => new SelectListItem
                        {
                            Value = c.CategoryID.ToString(),
                            Text = c.CategoryName
                        })
                        .ToList()
                }
            };

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            _logger.LogError(ex,
                "Unable to load categories for event {EventID}",
                eventId);

            TempData["ErrorMessage"] =
                "Unable to load event categories.";

            return RedirectToAction("Index", "Organiser");
        }
    }

    [HttpPost("Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        int eventId,
        AddEventCategoryViewModel model)
    {
        if (eventId <= 0)
            return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "Please check the category details.";

            return RedirectToAction(nameof(Index),
                new { eventId });
        }

        try
        {
            using var response = await _authenticatedApi
                .PostAsync(
                    $"api/events/{eventId}/categories",
                    new
                    {
                        model.CategoryID,
                        model.EntryFee,
                        model.MaxParticipants
                    });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] =
                    "Your API session has expired.";

                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["ErrorMessage"] =
                    "This category is already assigned to the event.";

                return RedirectToAction(nameof(Index),
                    new { eventId });
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to assign category.";

                return RedirectToAction(nameof(Index),
                    new { eventId });
            }

            TempData["SuccessMessage"] =
                "Category assigned successfully!";

            return RedirectToAction(nameof(Index),
                new { eventId });
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
                "Failed to assign category to event {EventID}",
                eventId);

            TempData["ErrorMessage"] =
                "Unable to connect to the API.";

            return RedirectToAction(nameof(Index),
                new { eventId });
        }
    }
}
