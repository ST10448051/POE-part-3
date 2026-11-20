using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net;

namespace RaceDay.MVC.Controllers;

[Authorize(Roles = "Participant")]
public class EnrolmentsController : Controller
{
    private readonly RaceDayApiService _api;
    private readonly AuthenticatedApiService _authenticatedApi;
    private readonly ILogger<EnrolmentsController> _logger;

    public EnrolmentsController(
        RaceDayApiService api,
        AuthenticatedApiService authenticatedApi,
        ILogger<EnrolmentsController> logger)
    {
        _api = api;
        _authenticatedApi = authenticatedApi;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int eventId)
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

            if (raceEvent.Status != "Open")
            {
                TempData["ErrorMessage"] =
                    "This event is not open for enrolment.";

                return RedirectToAction(
                    "Details", "Events",
                    new { id = eventId });
            }

            if (raceEvent.EventDate.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] =
                    "You cannot enrol in a past event.";

                return RedirectToAction(
                    "Details", "Events",
                    new { id = eventId });
            }

            var categories = await _api
                .GetListAsync<EventCategoryViewModel>(
                    $"api/events/{eventId}/categories");

            var model = new CreateEnrolmentViewModel
            {
                EventID = eventId,
                EventName = raceEvent.EventName,
                Categories = categories
            };

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            _logger.LogError(ex,
                "Failed to load enrolment form for event {EventID}",
                eventId);

            TempData["ErrorMessage"] =
                "Unable to load enrolment information.";

            return RedirectToAction("Index", "Events");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateEnrolmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategories(model);
            return View(model);
        }

        try
        {
            using var response = await _authenticatedApi
                .PostAsync(
                    "api/enrolments",
                    new
                    {
                        model.EventCategoryID
                    });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] =
                    "Your API session has expired. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                ModelState.AddModelError("",
                    "The selected race category no longer exists.");

                await PopulateCategories(model);
                return View(model);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError("",
                    "You may already be enrolled in this category, " +
                    "or the category may be full.");

                await PopulateCategories(model);
                return View(model);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError("",
                    "This event is not currently accepting enrolments.");

                await PopulateCategories(model);
                return View(model);
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Unable to complete enrolment.");

                await PopulateCategories(model);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "You have successfully enrolled in the event!";

            return RedirectToAction(nameof(My));
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
                "Failed to enrol participant in category {CategoryID}",
                model.EventCategoryID);

            ModelState.AddModelError("",
                "Unable to connect to the RaceDay API.");

            await PopulateCategories(model);
            return View(model);
        }
    }


    [HttpGet]
    public async Task<IActionResult> My()
    {
        try
        {
            using var response = await _authenticatedApi
                .GetAsync("api/enrolments/my");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to retrieve your enrolments.";

                return View(new List<MyEnrolmentViewModel>());
            }

            var enrolments = await response.Content
                .ReadFromJsonAsync<List<MyEnrolmentViewModel>>(
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            var model = (enrolments ?? new())
                .OrderByDescending(e => e.EnrolmentDate)
                .ToList();

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
                "Failed to retrieve participant enrolments.");

            TempData["ErrorMessage"] =
                "Unable to connect to the RaceDay API.";

            return View(new List<MyEnrolmentViewModel>());
        }
    }


    private async Task PopulateCategories(
        CreateEnrolmentViewModel model)
    {
        try
        {
            var raceEvent = await _api
                .GetAsync<EventViewModel>(
                    $"api/events/{model.EventID}");

            model.EventName =
                raceEvent?.EventName ?? "Race Event";

            model.Categories = await _api
                .GetListAsync<EventCategoryViewModel>(
                    $"api/events/{model.EventID}/categories");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Unable to reload categories for event {EventID}",
                model.EventID);

            model.Categories = new();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            using var response = await _authenticatedApi
                .DeleteAsync($"api/enrolments/{id}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["ErrorMessage"] =
                    "Enrolment not found.";

                return RedirectToAction(nameof(My));
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["ErrorMessage"] =
                    "This enrolment cannot be cancelled. " +
                    "It may already be cancelled or completed.";

                return RedirectToAction(nameof(My));
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to cancel your enrolment.";

                return RedirectToAction(nameof(My));
            }

            TempData["SuccessMessage"] =
                "Your enrolment has been cancelled successfully.";

            return RedirectToAction(nameof(My));
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
                "Failed to cancel enrolment {EnrolmentID}", id);

            TempData["ErrorMessage"] =
                "Unable to connect to the RaceDay API.";

            return RedirectToAction(nameof(My));
        }
    }


}
