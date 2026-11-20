using RaceDay.MVC.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace RaceDay.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = _httpClientFactory
            .CreateClient("RaceDayAPI");

        HttpResponseMessage response;

        try
        {
            response = await client.PostAsJsonAsync(
                "api/auth/login",
                new
                {
                    model.Email,
                    model.Password
                });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("",
                "Unable to connect to RaceDay. Please try again.");
            return View(model);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError("",
                "The login request timed out.");
            return View(model);
        }

        using (response)
        {
            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("",
                    "Invalid email or password.");
                return View(model);
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Login failed. Please try again.");
                return View(model);
            }

            LoginResponse? user;

            try
            {
                user = await response.Content
                    .ReadFromJsonAsync<LoginResponse>();
            }
            catch (System.Text.Json.JsonException)
            {
                ModelState.AddModelError("",
                    "Unexpected response from the server.");
                return View(model);
            }

            if (user == null ||
                user.UserID <= 0 ||
                string.IsNullOrWhiteSpace(user.FirstName) ||
                (user.Role != "Organiser" &&
                 user.Role != "Participant"))
            {
                ModelState.AddModelError("",
                    "Invalid account information returned.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserID.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.FirstName),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            if (!response.Headers.TryGetValues(
                    "Set-Cookie", out var cookies))
            {
                ModelState.AddModelError("",
                    "The API did not create an authentication session.");

                return View(model);
            }

            var authCookie = cookies
                .SelectMany(c => c.Split(';', 2))
                .FirstOrDefault(c =>
                    c.TrimStart().StartsWith(
                        "RaceDay.Auth=",
                        StringComparison.Ordinal));

            if (string.IsNullOrWhiteSpace(authCookie))
            {
                ModelState.AddModelError("",
                    "The API authentication cookie was not found.");

                return View(model);
            }

            _apiSession.SetApiCookie(authCookie.Trim());

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal);

            if (user.Role == "Organiser")
            {
                return RedirectToAction(
                    "Index", "Organiser");
            }

            return RedirectToAction(
                "Index", "Participant");
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        _apiSession.Clear();
        HttpContext.Session.Clear();

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }


    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Role != "Organiser" &&
            model.Role != "Participant")
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "Please select a valid role.");

            return View(model);
        }

        var client = _httpClientFactory
            .CreateClient("RaceDayAPI");

        try
        {
            using var response = await client.PostAsJsonAsync(
                "api/auth/register",
                new
                {
                    model.FirstName,
                    model.LastName,
                    model.Email,
                    model.Password,
                    model.Role
                });

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Conflict)
            {
                ModelState.AddModelError("",
                    "An account with this email already exists.");

                return View(model);
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Registration failed. Please check your details.");

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Registration successful! Please log in.";

            return RedirectToAction("Login");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("",
                "Unable to connect to RaceDay. Please try again.");

            return View(model);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError("",
                "The registration request timed out.");

            return View(model);
        }
    }
    private readonly ApiSessionService _apiSession;

    public AccountController(
        IHttpClientFactory httpClientFactory,
        ApiSessionService apiSession)
    {
        _httpClientFactory = httpClientFactory;
        _apiSession = apiSession;
    }

}
