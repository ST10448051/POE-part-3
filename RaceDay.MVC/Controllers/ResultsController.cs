
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers;

public class ResultsController : Controller
{
    private readonly AuthenticatedApiService _api;
    private readonly IHttpClientFactory _httpClientFactory;

    public ResultsController(
        AuthenticatedApiService api,
        IHttpClientFactory httpClientFactory)
    {
        _api = api;
        _httpClientFactory = httpClientFactory;
    }

    // Public event leaderboard
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Event(int eventId)
    {
        if (eventId <= 0)
            return NotFound();

        var client = _httpClientFactory.CreateClient("RaceDayAPI");

        using var eventResponse =
            await client.GetAsync($"api/events/{eventId}");

        if (!eventResponse.IsSuccessStatusCode)
            return NotFound();

        var raceEvent = await eventResponse.Content
            .ReadFromJsonAsync<EventViewModel>();

        if (raceEvent == null)
            return NotFound();

        using var response = await client.GetAsync(
            $"api/results/event/{eventId}");

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var results = await response.Content
            .ReadFromJsonAsync<List<ResultViewModel>>()
            ?? new List<ResultViewModel>();

        var model = new EventResultsViewModel
        {
            EventID = eventId,
            EventName = raceEvent.EventName,
            Results = results
                .OrderBy(r => r.Position ?? int.MaxValue)
                .ToList()
        };

        return View(model);
    }

    // Participant's personal results
    [HttpGet]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> My()
    {
        using var response =
            await _api.GetAsync("api/results/my");

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return RedirectToAction("Login", "Account");

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] =
                "Unable to load your race results.";

            return RedirectToAction("Index", "Participant");
        }

        var results = await response.Content
            .ReadFromJsonAsync<List<MyResultViewModel>>()
            ?? new List<MyResultViewModel>();

        return View(results);
    }

    // Organiser: record result form
    [HttpGet]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int eventId)
    {
        if (eventId <= 0)
            return NotFound();

        var model = new CreateResultPageViewModel
        {
            EventID = eventId,
            Result = new ResultFormViewModel
            {
                EventID = eventId,
                ResultStatus = "Finished"
            }
        };

        if (!await PopulateParticipants(model))
            return NotFound();

        return View(model);
    }

    // Organiser: save new result
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(
        CreateResultPageViewModel model)
    {
        model.Result.EventID = model.EventID;

        if (!ModelState.IsValid)
        {
            if (!await PopulateParticipants(model))
                return NotFound();

            return View(model);
        }

        if (!ValidResult(model.Result))
        {
            ModelState.AddModelError(
                "",
                "Finished results require a valid finish time " +
                "and position. Other statuses require neither.");

            if (!await PopulateParticipants(model))
                return NotFound();

            return View(model);
        }

        var dto = new
        {
            model.Result.EnrolmentID,
            model.Result.FinishTime,
            model.Result.Position,
            model.Result.ResultStatus
        };

        using var response =
            await _api.PostAsync("api/results", dto);

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] =
                "Race result recorded successfully.";

            return RedirectToAction(
                nameof(Event),
                new { eventId = model.EventID });
        }

        ModelState.AddModelError(
            "",
            await ReadApiError(response));

        if (!await PopulateParticipants(model))
            return NotFound();

        return View(model);
    }

    // Organiser: edit existing result
    [HttpGet]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Edit(
        int id, int eventId)
    {
        if (id <= 0 || eventId <= 0)
            return NotFound();

        // Verify that this result belongs to the
        // requested event before displaying the form.
        if (!await ResultBelongsToEvent(id, eventId))
            return NotFound();

        var client = _httpClientFactory.CreateClient("RaceDayAPI");

        using var response =
            await client.GetAsync($"api/results/{id}");

        if (!response.IsSuccessStatusCode)
            return NotFound();

        var result = await response.Content
            .ReadFromJsonAsync<ResultViewModel>();

        if (result == null)
            return NotFound();

        var model = new ResultFormViewModel
        {
            ResultID = result.ResultID,
            EventID = eventId,
            EnrolmentID = result.EnrolmentID,
            FinishTime = result.FinishTime,
            Position = result.Position,
            ResultStatus = result.ResultStatus
        };

        return View(model);
    }

    // Organiser: save edited result
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Edit(
        int id, ResultFormViewModel model)
    {
        if (id != model.ResultID)
            return BadRequest();

        if (!ModelState.IsValid || !ValidResult(model))
        {
            if (!ValidResult(model))
                ModelState.AddModelError(
                    "",
                    "Invalid result details.");

            return View(model);
        }

        if (!await ResultBelongsToEvent(id, model.EventID))
            return NotFound();

        var dto = new
        {
            model.FinishTime,
            model.Position,
            model.ResultStatus
        };

        using var response =
            await _api.PutAsync($"api/results/{id}", dto);

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] =
                "Race result updated successfully.";

            return RedirectToAction(
                nameof(Event),
                new { eventId = model.EventID });
        }

        ModelState.AddModelError(
            "",
            await ReadApiError(response));

        return View(model);
    }

    // Organiser: delete result
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(
        int id, int eventId)
    {
        if (!await ResultBelongsToEvent(id, eventId))
            return NotFound();

        using var response =
            await _api.DeleteAsync($"api/results/{id}");

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] =
                "Race result deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] =
                await ReadApiError(response);
        }

        return RedirectToAction(
            nameof(Event),
            new { eventId });
    }

    private async Task<bool> PopulateParticipants(
        CreateResultPageViewModel model)
    {
        using var response = await _api.GetAsync(
            $"api/events/{model.EventID}/participants");

        if (!response.IsSuccessStatusCode)
            return false;

        var eventData = await response.Content
            .ReadFromJsonAsync<EventParticipantsViewModel>();

        if (eventData == null)
            return false;

        model.EventName = eventData.EventName;

        var client = _httpClientFactory.CreateClient("RaceDayAPI");

        using var resultsResponse = await client.GetAsync(
            $"api/results/event/{model.EventID}");

        if (!resultsResponse.IsSuccessStatusCode)
            return false;

        var existingResults = await resultsResponse.Content
            .ReadFromJsonAsync<List<ResultViewModel>>()
            ?? new List<ResultViewModel>();

        var completedEnrolmentIds = existingResults
            .Select(r => r.EnrolmentID)
            .ToHashSet();

        model.Participants = eventData.Participants
            .Where(p =>
                p.Status != "Cancelled" &&
                !completedEnrolmentIds.Contains(p.EnrolmentID))
            .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = p.EnrolmentID.ToString(),
                Text = $"{p.FullName} — {p.CategoryName}"
            })
            .ToList();

        return true;
    }

    private async Task<bool> ResultBelongsToEvent(
        int resultId, int eventId)
    {
        if (resultId <= 0 || eventId <= 0)
            return false;

        var client = _httpClientFactory.CreateClient("RaceDayAPI");

        using var response = await client.GetAsync(
            $"api/results/event/{eventId}");

        if (!response.IsSuccessStatusCode)
            return false;

        var results = await response.Content
            .ReadFromJsonAsync<List<ResultViewModel>>()
            ?? new List<ResultViewModel>();

        return results.Any(r => r.ResultID == resultId);
    }

    private static bool ValidResult(ResultFormViewModel model)
    {
        if (model.ResultStatus == "Finished")
        {
            return model.FinishTime.HasValue &&
                   model.FinishTime.Value > TimeSpan.Zero &&
                   model.FinishTime.Value < TimeSpan.FromDays(1) &&
                   model.Position.HasValue &&
                   model.Position.Value > 0;
        }

        return (model.ResultStatus == "DidNotFinish" ||
                model.ResultStatus == "Disqualified") &&
               model.FinishTime == null &&
               model.Position == null;
    }

    private static async Task<string> ReadApiError(
        HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Conflict)
            return "A result already exists for this enrolment, " +
                   "or the operation conflicts with its current status.";

        if (response.StatusCode == HttpStatusCode.Forbidden)
            return "You do not have permission to manage this result.";

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return "Your API session has expired. Please log in again.";

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return "The result details were rejected by the API.";

        return "Unable to save the race result. Please try again.";
    }
}
