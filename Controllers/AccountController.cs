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
            // Connect to the Part 2 API.
            _api = httpClientFactory.CreateClient("RaceDayAPI");
        }

        // Display the registration page.
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Send registration information to the API.
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

        // Send login information to the API.
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
                    TempData["SuccessMessage"] =
                        "Login request successful.";

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