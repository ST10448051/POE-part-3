
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers;

[Authorize(Roles = "Organiser")]
public class CategoriesController : Controller
{
    private readonly RaceDayApiService _apiService;
    private readonly AuthenticatedApiService _authenticatedApi;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        RaceDayApiService apiService,
        AuthenticatedApiService authenticatedApi,
        ILogger<CategoriesController> logger)
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
            var categories = await _apiService
                .GetListAsync<CategoryViewModel>(
                    "api/categories");

            return View(categories);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Unable to load race categories.");

            TempData["ErrorMessage"] =
                "Unable to load categories.";

            return View(new List<CategoryViewModel>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CategoryViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            using var response = await _authenticatedApi
                .PostAsync("api/categories", new
                {
                    model.CategoryName,
                    model.Description
                });

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("",
                    "Your API session has expired. Please log in again.");

                return View(model);
            }

            if (response.StatusCode ==
                HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            if (response.StatusCode ==
                HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "This category already exists.");

                return View(model);
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Unable to create category.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Category created successfully!";

            return RedirectToAction(nameof(Index));
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
                "Failed to create race category.");

            ModelState.AddModelError("",
                "Unable to connect to the API.");

            return View(model);
        }
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var category = await _apiService
                .GetAsync<CategoryViewModel>(
                    $"api/categories/{id}");

            if (category == null)
                return NotFound();

            return View(category);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            _logger.LogError(ex,
                "Failed to load category {CategoryID}", id);

            TempData["ErrorMessage"] =
                "Unable to load the category.";

            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, CategoryViewModel model)
    {
        if (id <= 0 || id != model.CategoryID)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            using var response = await _authenticatedApi
                .PutAsync($"api/categories/{id}", new
                {
                    model.CategoryName,
                    model.Description
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("",
                    "Your API session has expired. Please log in again.");

                return View(model);
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Another category already uses this name.");

                return View(model);
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Unable to update the category.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Category updated successfully!";

            return RedirectToAction(nameof(Index));
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
                "Failed to update category {CategoryID}", id);

            ModelState.AddModelError("",
                "Unable to connect to the RaceDay API.");

            return View(model);
        }
    }


    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            var category = await _apiService
                .GetAsync<CategoryViewModel>(
                    $"api/categories/{id}");

            if (category == null)
                return NotFound();

            return View(category);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            _logger.LogError(ex,
                "Failed to load category {CategoryID}", id);

            TempData["ErrorMessage"] =
                "Unable to load the category.";

            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
            using var response = await _authenticatedApi
                .DeleteAsync($"api/categories/{id}");

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

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["ErrorMessage"] =
                    "This category cannot be deleted because it is assigned to an event.";

                return RedirectToAction(nameof(Index));
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] =
                    "Unable to delete the category.";

                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Category deleted successfully!";

            return RedirectToAction(nameof(Index));
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
                "Failed to delete category {CategoryID}", id);

            TempData["ErrorMessage"] =
                "Unable to connect to the RaceDay API.";

            return RedirectToAction(nameof(Index));
        }
    }


}
