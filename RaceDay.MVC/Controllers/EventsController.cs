
using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class EventsController : Controller
{
    private readonly RaceDayApiService _apiService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        RaceDayApiService apiService,
        AuthenticatedApiService authenticatedApi,
        ILogger<EventsController> logger)
    {
        _apiService = apiService;
        _authenticatedApi = authenticatedApi;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var events = await _apiService
                .GetListAsync<EventViewModel>("api/events");

            var upcomingEvents = events
                .Where(e =>
                    e.EventDate.Date >= DateTime.Today &&
                    (e.Status == "Upcoming" ||
                     e.Status == "Open"))
                .OrderBy(e => e.EventDate)
                .ToList();

            return View(upcomingEvents);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Unable to retrieve events from API.");

            TempData["ErrorMessage"] =
                "Unable to load events. Please try again.";

            return View(new List<EventViewModel>());
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex,
                "Invalid event data returned by API.");

            TempData["ErrorMessage"] =
                "Unable to display event information.";

            return View(new List<EventViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        try
        {
            var raceEvent = await _apiService
                .GetAsync<EventViewModel>(
                    $"api/events/{id}");

            if (raceEvent == null)
            {
                return NotFound();
            }

            return View(raceEvent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to retrieve event {EventID}", id);

            TempData["ErrorMessage"] =
                "Unable to load event details.";

            return RedirectToAction(nameof(Index));
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex,
                "Invalid event details for {EventID}", id);

            TempData["ErrorMessage"] =
                "Unable to display event details.";

            return RedirectToAction(nameof(Index));
        }
    }

    private readonly AuthenticatedApiService _authenticatedApi;

    [HttpGet]
    [Authorize(Roles = "Organiser")]
    public IActionResult Create()
    {
        return View(new EventFormViewModel
        {
            EventDate = DateTime.Today.AddDays(7),
            Status = "Draft"
        });
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            using var response = await _authenticatedApi
                .PostAsync("api/events", new
                {
                    model.EventName,
                    model.Description,
                    model.EventDate,
                    model.Location,
                    model.DistanceKm,
                    model.EntryFee,
                    model.EventType,
                    model.Status
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("",
                    "Your API session has expired. Please log in again.");

                return View(model);
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Unable to create event. Please check your details.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Event created successfully!";

            return RedirectToAction("Index", "Organiser");
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError("",
                "Your session has expired. Please log in again.");

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Error communicating with RaceDay API.");

            ModelState.AddModelError("",
                "Unable to connect to the server.");

            return View(model);
        }
    }


    [HttpGet]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Edit(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var raceEvent = await _apiService
                .GetAsync<EventViewModel>(
                    $"api/events/{id}");

            if (raceEvent == null)
                return NotFound();

            var model = new EventFormViewModel
            {
                EventID = raceEvent.EventID,
                EventName = raceEvent.EventName,
                Description = raceEvent.Description,
                EventDate = raceEvent.EventDate,
                Location = raceEvent.Location,
                DistanceKm = raceEvent.DistanceKm,
                EntryFee = raceEvent.EntryFee,
                EventType = raceEvent.EventType,
                Status = raceEvent.Status
            };

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Unable to load event {EventID}", id);

            TempData["ErrorMessage"] =
                "Unable to load the event.";

            return RedirectToAction("Index", "Organiser");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, EventFormViewModel model)
    {
        if (id <= 0 || id != model.EventID)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            using var response = await _authenticatedApi
                .PutAsync($"api/events/{id}", new
                {
                    model.EventName,
                    model.Description,
                    model.EventDate,
                    model.Location,
                    model.DistanceKm,
                    model.EntryFee,
                    model.EventType,
                    model.Status
                });

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("",
                    "Your session has expired. Please log in again.");

                return View(model);
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Unable to update the event.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Event updated successfully!";

            return RedirectToAction("Index", "Organiser");
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError("",
                "Your API session has expired.");

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to update event {EventID}", id);

            ModelState.AddModelError("",
                "Unable to connect to the server.");

            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var raceEvent = await _apiService
                .GetAsync<EventViewModel>(
                    $"api/events/{id}");

            if (raceEvent == null)
                return NotFound();

            return View(raceEvent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Unable to load event {EventID} for deletion", id);

            TempData["ErrorMessage"] =
                "Unable to load the event.";

            return RedirectToAction("Index", "Organiser");
        }
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Organiser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            using var response = await _authenticatedApi
                .DeleteAsync($"api/events/{id}");

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete the event.";

                return RedirectToAction("Index", "Organiser");
            }

            TempData["SuccessMessage"] =
                "Event deleted successfully!";

            return RedirectToAction("Index", "Organiser");
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
                "Failed to delete event {EventID}", id);

            TempData["ErrorMessage"] =
                "Unable to connect to the server.";

            return RedirectToAction("Index", "Organiser");
        }
    }


}
