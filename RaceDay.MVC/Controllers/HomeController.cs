using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Diagnostics;

namespace RaceDay.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        private readonly RaceDay.MVC.Services.RaceDayApiService
            _apiService;

        // Add RaceDayApiService to your existing
        // HomeController constructor and assign it
        // to _apiService.

        public async Task<IActionResult> TestApi()
        {
            try
            {
                var events = await _apiService
                    .GetListAsync<System.Text.Json.JsonElement>(
                        "api/events");

                return Json(events);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Unable to communicate with RaceDay API.");
            }
            catch (System.Text.Json.JsonException)
            {
                return StatusCode(502,
                    "The API returned an unexpected response.");
            }
        }
    }
}
