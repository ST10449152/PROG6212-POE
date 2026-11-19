using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    [SessionAuthorize("Organiser")]
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public OrganiserController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // Display only the logged-in organiser's events.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var myEvents = new List<EventViewModel>();
            int? organiserId = HttpContext.Session.GetInt32("UserID");

            if (organiserId == null)
                return RedirectToAction("Login", "Account");

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get, "api/events");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Account");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = await ReadError(response);
                    return View(myEvents);
                }

                var events = await response.Content
                    .ReadFromJsonAsync<List<EventViewModel>>();

                myEvents = (events ?? new List<EventViewModel>())
                    .Where(e => e.OrganiserID == organiserId.Value)
                    .OrderBy(e => e.EventDate)
                    .ToList();
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                ViewBag.ErrorMessage =
                    "Unable to load events from RaceDayAPI.";
            }

            return View(myEvents);
        }

        // Display the Create Event form.
        [HttpGet]
        public IActionResult Create()
        {
            return View(new EventFormViewModel());
        }

        // Save a new event through RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            ValidateEvent(model);

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Post, "api/events", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event created successfully.";

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "", await ReadError(response));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDayAPI.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View(model);
        }

        // Display an existing event for editing.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await LoadOwnedEvent(id);

            if (result.Event == null)
                return result.Error!;

            return View(result.Event);
        }

        // Update the event through RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, EventFormViewModel model)
        {
            if (id != model.EventID)
                return BadRequest();

            ValidateEvent(model);

            if (!ModelState.IsValid)
                return View(model);

            var ownedEvent = await LoadOwnedEvent(id);

            if (ownedEvent.Event == null)
                return ownedEvent.Error!;

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Put, $"api/events/{id}", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event updated successfully.";

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "", await ReadError(response));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDayAPI.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View(model);
        }

        // Display the Delete Event confirmation.
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await LoadOwnedEvent(id);

            if (result.Event == null)
                return result.Error!;

            return View(result.Event);
        }

        // Delete an event through RaceDayAPI.
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await LoadOwnedEvent(id);

            if (result.Event == null)
                return result.Error!;

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Delete, $"api/events/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event deleted successfully.";

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "", await ReadError(response));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDayAPI.");
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "The API request timed out.");
            }

            return View("Delete", result.Event);
        }

        // Check event details before sending them to the API.
        private void ValidateEvent(EventFormViewModel model)
        {
            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "End time must be after start time.");
            }

            if (model.EventType != "Run" &&
                model.EventType != "Walk" &&
                model.EventType != "Cycle")
            {
                ModelState.AddModelError(
                    nameof(model.EventType),
                    "Choose Run, Walk or Cycle.");
            }
        }

        // Load an event and check that this organiser owns it.
        private async Task<(EventFormViewModel? Event,
            IActionResult? Error)> LoadOwnedEvent(int id)
        {
            int? organiserId = HttpContext.Session.GetInt32("UserID");

            if (organiserId == null)
                return (null, RedirectToAction("Login", "Account"));

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get, $"api/events/{id}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return (null, NotFound("Event not found."));

                if (!response.IsSuccessStatusCode)
                    return (null, StatusCode(502,
                        "Unable to retrieve the event."));

                var selectedEvent = await response.Content
                    .ReadFromJsonAsync<EventFormViewModel>();

                if (selectedEvent == null)
                    return (null, NotFound());

                // MVC checks ownership before showing management forms.
                // RaceDayAPI also checks ownership when saving changes.
                using var ownershipResponse = await SendRequest(
                    HttpMethod.Get, "api/events");

                if (!ownershipResponse.IsSuccessStatusCode)
                    return (null, StatusCode(502,
                        "Unable to verify event ownership."));

                var events = await ownershipResponse.Content
                    .ReadFromJsonAsync<List<EventViewModel>>();

                bool isOwner = events?.Any(e =>
                    e.EventID == id &&
                    e.OrganiserID == organiserId.Value) == true;

                if (!isOwner)
                    return (null, StatusCode(403,
                        "You can only manage your own events."));

                return (selectedEvent, null);
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                return (null, StatusCode(502,
                    "Unable to connect to RaceDayAPI."));
            }
        }

        // Forward API requests with the logged-in user's cookie.
        private async Task<HttpResponseMessage> SendRequest(
            HttpMethod method, string url, object? data = null)
        {
            using var request = new HttpRequestMessage(method, url);

            if (data != null)
                request.Content = JsonContent.Create(data);

            return await _apiService.SendAsync(request);
        }

        // Read plain-text or JSON API error messages.
        private static async Task<string> ReadError(
            HttpResponseMessage response)
        {
            string message = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(message))
                return "The API could not complete the request.";

            return message;
        }
    }
}