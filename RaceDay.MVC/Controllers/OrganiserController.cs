
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserController : Controller
{
    private readonly AuthenticatedApiService _apiService;
    private readonly ILogger<OrganiserController> _logger;

    public OrganiserController(
        AuthenticatedApiService apiService,
        ILogger<OrganiserController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            using var response = await _apiService
                .GetAsync("api/events/mine");

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                TempData["ErrorMessage"] =
                    "Your session has expired. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode ==
                HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to load your events.";

                return View(new List<EventViewModel>());
            }

            var events = await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>();

            return View(events ?? new List<EventViewModel>());
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Unable to load Organiser events.");

            TempData["ErrorMessage"] =
                "Unable to connect to the RaceDay API.";

            return View(new List<EventViewModel>());
        }
    }
}
