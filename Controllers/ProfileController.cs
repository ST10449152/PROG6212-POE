using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class ProfileController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public ProfileController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // Display the logged-in user's profile.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Account");

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "api/users/me");

                using var response = await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to load your profile.";
                    return View(null);
                }

                var profile = await response.Content
                    .ReadFromJsonAsync<UserProfileViewModel>();

                if (profile == null)
                {
                    ViewBag.ErrorMessage =
                        "The API returned an empty profile.";
                    return View(null);
                }

                return View(profile);
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to RaceDayAPI.";
                return View(null);
            }
        }

        // Display the Edit Profile form.
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Account");

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "api/users/me");

                using var response = await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to load your profile.");

                var profile = await response.Content
                    .ReadFromJsonAsync<UserProfileViewModel>();

                if (profile == null)
                    return NotFound("Profile information is unavailable.");

                var model = new EditProfileViewModel
                {
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    Email = profile.Email
                };

                return View(model);
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                return StatusCode(502,
                    "Unable to connect to RaceDayAPI.");
            }
        }

        // Save updated profile details through RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Put, "api/users/me");

                request.Content = JsonContent.Create(new
                {
                    model.FirstName,
                    model.LastName,
                    model.Email
                });

                using var response = await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.IsSuccessStatusCode)
                {
                    // Update the name displayed in the navigation bar.
                    HttpContext.Session.SetString(
                        "FirstName", model.FirstName);

                    TempData["SuccessMessage"] =
                        "Your profile was updated successfully.";

                    return RedirectToAction(nameof(Index));
                }

                string errorMessage =
                    await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    "",
                    string.IsNullOrWhiteSpace(errorMessage)
                        ? "Unable to update your profile."
                        : errorMessage);
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDayAPI.");
            }

            return View(model);
        }

        // Clear an expired session and redirect to login.
        private IActionResult SessionExpired()
        {
            HttpContext.Session.Clear();

            TempData["SuccessMessage"] =
                "Your session has expired. Please log in again.";

            return RedirectToAction("Login", "Account");
        }
    }
}