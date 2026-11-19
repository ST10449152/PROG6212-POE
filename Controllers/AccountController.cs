using RaceDayMVC.Services;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net.Http.Json;

namespace RaceDayMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _api;
        private readonly RaceDayApiService _apiService;

        public AccountController(
            IHttpClientFactory httpClientFactory,
            RaceDayApiService apiService)
        {
            _api = httpClientFactory.CreateClient("RaceDayAPI");
            _apiService = apiService;
        }

        // Display the registration page.
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Send registration details to the API.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _api.PostAsJsonAsync(
                    "api/auth/register", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Registration successful. Please log in.";

                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("",
                    "Registration failed. Check your details or try another email.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("",
                    "Unable to connect to RaceDayAPI.");
            }

            return View(model);
        }

        // Display the login page.
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Send login details to the API.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using var response = await _api.PostAsJsonAsync(
                    "api/auth/login", model);

                if (response.IsSuccessStatusCode)
                {
                    // Read the user's details returned by RaceDayAPI.
                    var user = await response.Content
                        .ReadFromJsonAsync<LoginResponse>();

                    if (user == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to read the login response.");

                        return View(model);
                    }

                    // Retrieve the API session cookie.
                    if (!response.Headers.TryGetValues(
                        "Set-Cookie", out var cookies))
                    {
                        ModelState.AddModelError("",
                            "The API did not return a session cookie.");

                        return View(model);
                    }

                    var sessionCookie = cookies
                        .Select(cookie => cookie.Split(';')[0])
                        .FirstOrDefault(cookie =>
                            cookie.StartsWith(
                                ".AspNetCore.Session=",
                                StringComparison.Ordinal));

                    if (string.IsNullOrEmpty(sessionCookie))
                    {
                        ModelState.AddModelError("",
                            "The API session cookie was not found.");

                        return View(model);
                    }

                    // Remove details from any previous login.
                    HttpContext.Session.Clear();

                    // Store the user's account information.
                    HttpContext.Session.SetInt32(
                        "UserID", user.UserID);

                    HttpContext.Session.SetString(
                        "FirstName", user.FirstName);

                    HttpContext.Session.SetString(
                        "Role", user.Role);

                    // Store the API cookie for authenticated requests.
                    HttpContext.Session.SetString(
                        "ApiSessionCookie", sessionCookie);

                    // Organisers can choose between two interfaces.
                    if (user.Role == "Organiser")
                    {
                        return RedirectToAction("Index", "Mode");
                    }

                    // Participants go directly to Participant Home.
                    if (user.Role == "Participant")
                    {
                        HttpContext.Session.SetString(
                            "SelectedMode", "Participant");

                        return RedirectToAction("Index", "Participant");
                    }

                    // Do not allow an unknown role into the application.
                    HttpContext.Session.Clear();

                    ModelState.AddModelError("",
                        "Your account has an unsupported role.");

                    return View(model);
                }

                ModelState.AddModelError("",
                    "Invalid email or password.");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("",
                    "Unable to connect to RaceDayAPI.");
            }

            return View(model);
        }

        // Log out of RaceDayMVC and RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Ask RaceDayAPI to end the user's API session.
                using var request = new HttpRequestMessage(
                    HttpMethod.Post, "api/auth/logout");

                using var response =
                    await _apiService.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                // Still log out locally if the API is unavailable.
            }
            finally
            {
                // Clear account details, selected mode and API cookie.
                HttpContext.Session.Clear();
            }

            return RedirectToAction("Login", "Account");
        }
    }
}