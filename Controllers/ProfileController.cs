using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Net;
using System.Net.Http.Json;

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
            // Check whether the MVC user is logged in.
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Request the user's profile from RaceDayAPI.
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "api/users/me");

                using var response =
                    await _apiService.SendAsync(request);

                // The API session is no longer valid.
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();

                    TempData["SuccessMessage"] =
                        "Your session has expired. Please log in again.";

                    return RedirectToAction("Login", "Account");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to load your profile.";

                    return View(null);
                }

                // Read the profile returned by the API.
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
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to RaceDayAPI.";

                return View(null);
            }
        }
    }
}