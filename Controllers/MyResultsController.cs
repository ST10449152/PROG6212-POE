using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize("Participant")]
    public class MyResultsController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public MyResultsController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /MyResults
        // Display the logged-in participant's race results.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<MyResultViewModel> results = new();

            try
            {
                // Retrieve the participant's results from the API.
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "api/results/me");

                using var response =
                    await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();

                    return RedirectToAction("Login", "Account");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return StatusCode(403,
                        "Only participants can view their race results.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to load your race results.";

                    return View(results);
                }

                // Read the results returned by RaceDayAPI.
                var apiResults = await response.Content
                    .ReadFromJsonAsync<List<MyResultViewModel>>();

                if (apiResults != null)
                {
                    results = apiResults;

                    // Retrieve event details for each result.
                    foreach (var result in results)
                    {
                        using var eventRequest = new HttpRequestMessage(
                            HttpMethod.Get,
                            $"api/events/{result.EventID}");

                        using var eventResponse =
                            await _apiService.SendAsync(eventRequest);

                        if (eventResponse.IsSuccessStatusCode)
                        {
                            var eventDetails = await eventResponse.Content
                                .ReadFromJsonAsync<EventViewModel>();

                            if (eventDetails != null)
                            {
                                result.EventName = eventDetails.EventName;
                                result.EventDate = eventDetails.EventDate;
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to RaceDayAPI.";
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "Unable to read your race results.";
            }

            return View(results);
        }
    }
}