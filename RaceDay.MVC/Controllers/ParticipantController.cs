
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.MVC.Controllers;

[Authorize(Roles = "Participant")]
public class ParticipantController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    private readonly RaceDay.MVC.Services.AuthenticatedApiService
    _apiService;

    public ParticipantController(
        RaceDay.MVC.Services.AuthenticatedApiService apiService)
    {
        _apiService = apiService;
    }
    public async Task<IActionResult> TestAuthentication()
    {
        using var response = await _apiService.GetAsync(
            "api/auth/me");

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(
                (int)response.StatusCode,
                "API authentication failed.");
        }

        var result = await response.Content
            .ReadAsStringAsync();

        return Content(result, "application/json");
    }

}

