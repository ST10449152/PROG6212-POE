using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    // Both participants and organisers can browse races.
    [SessionAuthorize("Participant")]
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public ParticipantController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // Display upcoming races with optional filters.
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? eventType,
            string? location)
        {
            List<EventViewModel> upcomingEvents =
                new List<EventViewModel>();

            // Keep the entered filters when the page reloads.
            ViewBag.Search = search;
            ViewBag.EventType = eventType;
            ViewBag.Location = location;

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "api/events");

                using var response =
                    await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                if (response.IsSuccessStatusCode)
                {
                    var events = await response.Content
                        .ReadFromJsonAsync<List<EventViewModel>>();

                    if (events != null)
                    {
                        // Show only today's and future events.
                        var filteredEvents = events
                            .Where(e => e.EventDate.Date >= DateTime.Today);

                        // Filter by event name.
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            filteredEvents = filteredEvents.Where(e =>
                                e.EventName.Contains(
                                    search,
                                    StringComparison.OrdinalIgnoreCase));
                        }

                        // Filter by event type.
                        if (!string.IsNullOrWhiteSpace(eventType))
                        {
                            filteredEvents = filteredEvents.Where(e =>
                                e.EventType.Equals(
                                    eventType,
                                    StringComparison.OrdinalIgnoreCase));
                        }

                        // Filter by location.
                        if (!string.IsNullOrWhiteSpace(location))
                        {
                            filteredEvents = filteredEvents.Where(e =>
                                e.Location.Contains(
                                    location,
                                    StringComparison.OrdinalIgnoreCase));
                        }

                        upcomingEvents = filteredEvents
                            .OrderBy(e => e.EventDate)
                            .ToList();
                    }
                }
                else
                {
                    ViewBag.ErrorMessage =
                        "Unable to load race events.";
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
                    "Unable to read race information.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.ErrorMessage =
                    "The request took too long. Please try again.";
            }

            return View(upcomingEvents);
        }

        // GET: /Participant/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            EventDetailsViewModel model =
                new EventDetailsViewModel();

            try
            {
                // Get the selected race.
                using var eventRequest = new HttpRequestMessage(
                    HttpMethod.Get, $"api/events/{id}");

                using var eventResponse =
                    await _apiService.SendAsync(eventRequest);

                if (eventResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                if (eventResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound("The selected race was not found.");
                }

                if (!eventResponse.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to load race details.";

                    return View(model);
                }

                var selectedEvent = await eventResponse.Content
                    .ReadFromJsonAsync<EventDetailsApiResponse>();

                if (selectedEvent == null)
                {
                    ViewBag.ErrorMessage =
                        "The API returned no race information.";

                    return View(model);
                }

                model.Event = selectedEvent;
                model.RouteDescription =
                    selectedEvent.RouteDescription;
                model.RouteMapURL =
                    selectedEvent.RouteMapURL;

                // Get the available categories.
                using var categoryRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/events/{id}/categories");

                using var categoryResponse =
                    await _apiService.SendAsync(categoryRequest);

                if (categoryResponse.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                if (categoryResponse.IsSuccessStatusCode)
                {
                    model.Categories =
                        await categoryResponse.Content
                            .ReadFromJsonAsync<List<CategoryViewModel>>()
                        ?? new List<CategoryViewModel>();
                }
                else
                {
                    ViewBag.CategoryError =
                        "Unable to load race categories.";
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
                    "Unable to read the race details.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.ErrorMessage =
                    "The request took too long. Please try again.";
            }

            return View(model);
        }

        // POST: /Participant/Enrol
        // Enrol the logged-in participant in a category.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(
            int eventId, int categoryId)
        {
            if (eventId <= 0 || categoryId <= 0)
            {
                return BadRequest("Invalid event or category.");
            }

            // Organisers can browse but cannot enrol.
            string? role = HttpContext.Session.GetString("Role");

            if (role != "Participant")
            {
                return StatusCode(403,
                    "Only participants can enrol in races.");
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/events/{eventId}/enrolments");

                request.Content = JsonContent.Create(new
                {
                    CategoryID = categoryId
                });

                using var response =
                    await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "You have successfully enrolled in this race!";
                }
                else
                {
                    string error = await response.Content
                        .ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        TempData["ErrorMessage"] =
                            string.IsNullOrWhiteSpace(error)
                                ? "Unable to complete your enrolment."
                                : error;
                    }
                    else
                    {
                        TempData["ErrorMessage"] =
                            "Enrolment failed. Please try again.";
                    }
                }
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "Unable to connect to RaceDayAPI.";
            }
            catch (TaskCanceledException)
            {
                TempData["ErrorMessage"] =
                    "The request took too long. Please try again.";
            }

            return RedirectToAction("Details",
                new { id = eventId });
        }

        // The API returns extra route information.
        private class EventDetailsApiResponse : EventViewModel
        {
            public string RouteDescription { get; set; }
                = string.Empty;

            public string RouteMapURL { get; set; }
                = string.Empty;
        }
    }
}