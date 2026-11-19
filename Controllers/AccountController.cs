using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using System.Net.Http.Json;

namespace RaceDayMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _api;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            // Connect to RaceDayAPI.
            _api = httpClientFactory.CreateClient("RaceDayAPI");
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
                var response = await _api.PostAsJsonAsync(
                    "api/auth/login", model);

                if (response.IsSuccessStatusCode)
                {
                    // Read the user details returned by the API.
                    var user = await response.Content
                        .ReadFromJsonAsync<LoginResponse>();

                    if (user == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to read the login response.");

                        return View(model);
                    }

                    // Get the API session cookie.
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
                            cookie.StartsWith(".AspNetCore.Session=",
                                StringComparison.Ordinal));

                    if (string.IsNullOrEmpty(sessionCookie))
                    {
                        ModelState.AddModelError("",
                            "The API session cookie was not found.");

                        return View(model);
                    }

                    // Clear the previous MVC session.
                    HttpContext.Session.Clear();

                    // Save the logged-in user's details.
                    HttpContext.Session.SetInt32(
                        "UserID", user.UserID);

                    HttpContext.Session.SetString(
                        "FirstName", user.FirstName);

                    HttpContext.Session.SetString(
                        "Role", user.Role);

                    // Save the API session cookie for later requests.
                    HttpContext.Session.SetString(
                        "ApiSessionCookie", sessionCookie);

                    // Redirect to the home page for now.
                    return RedirectToAction("Index", "Home");
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
    }
}