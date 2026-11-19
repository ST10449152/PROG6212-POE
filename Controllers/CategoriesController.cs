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
    public class CategoriesController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public CategoriesController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // Display categories for an organiser's event.
        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            var error = await CheckEventOwnership(eventId);

            if (error != null)
                return error;

            ViewBag.EventID = eventId;

            var categories = new List<CategoryFormViewModel>();

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/categories");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = await ReadError(response);
                    return View(categories);
                }

                categories = await response.Content
                    .ReadFromJsonAsync<List<CategoryFormViewModel>>()
                    ?? new List<CategoryFormViewModel>();
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                ViewBag.ErrorMessage =
                    "Unable to load event categories.";
            }

            return View(categories);
        }

        // Display the Create Category form.
        [HttpGet]
        public async Task<IActionResult> Create(int eventId)
        {
            var error = await CheckEventOwnership(eventId);

            if (error != null)
                return error;

            return View(new CategoryFormViewModel
            {
                EventID = eventId
            });
        }

        // Create a category through RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CategoryFormViewModel model)
        {
            var error = await CheckEventOwnership(model.EventID);

            if (error != null)
                return error;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Post,
                    $"api/events/{model.EventID}/categories",
                    model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Category created successfully.";

                    return RedirectToAction(nameof(Index),
                        new { eventId = model.EventID });
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

        // Display the Edit Category form.
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int eventId)
        {
            var result = await LoadCategory(id, eventId);

            if (result.Category == null)
                return result.Error!;

            return View(result.Category);
        }

        // Update a category through RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, CategoryFormViewModel model)
        {
            if (id != model.CategoryID)
                return BadRequest();

            var result = await LoadCategory(id, model.EventID);

            if (result.Category == null)
                return result.Error!;

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Put, $"api/categories/{id}", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Category updated successfully.";

                    return RedirectToAction(nameof(Index),
                        new { eventId = model.EventID });
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

        // Display the Delete Category confirmation.
        [HttpGet]
        public async Task<IActionResult> Delete(int id, int eventId)
        {
            var result = await LoadCategory(id, eventId);

            if (result.Category == null)
                return result.Error!;

            return View(result.Category);
        }

        // Delete a category through RaceDayAPI.
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id, int eventId)
        {
            var result = await LoadCategory(id, eventId);

            if (result.Category == null)
                return result.Error!;

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Delete, $"api/categories/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Category deleted successfully.";

                    return RedirectToAction(nameof(Index),
                        new { eventId });
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

            return View("Delete", result.Category);
        }

        // Check that the event belongs to the current organiser.
        private async Task<IActionResult?> CheckEventOwnership(int eventId)
        {
            int? organiserId = HttpContext.Session.GetInt32("UserID");

            if (organiserId == null)
                return RedirectToAction("Login", "Account");

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get, "api/events");

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to verify event ownership.");

                var events = await response.Content
                    .ReadFromJsonAsync<List<EventViewModel>>();

                var selectedEvent = events?
                    .FirstOrDefault(e => e.EventID == eventId);

                if (selectedEvent == null)
                    return NotFound("Event not found.");

                if (selectedEvent.OrganiserID != organiserId.Value)
                    return StatusCode(403,
                        "You can only manage your own event categories.");

                return null;
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

        // Load a category from its event's category list.
        private async Task<(CategoryFormViewModel? Category,
            IActionResult? Error)> LoadCategory(int id, int eventId)
        {
            var error = await CheckEventOwnership(eventId);

            if (error != null)
                return (null, error);

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/categories");

                if (!response.IsSuccessStatusCode)
                    return (null, StatusCode(502,
                        "Unable to load categories."));

                var categories = await response.Content
                    .ReadFromJsonAsync<List<CategoryFormViewModel>>();

                var category = categories?.FirstOrDefault(
                    c => c.CategoryID == id && c.EventID == eventId);

                if (category == null)
                    return (null, NotFound("Category not found."));

                return (category, null);
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

        private async Task<HttpResponseMessage> SendRequest(
            HttpMethod method, string url, object? data = null)
        {
            using var request = new HttpRequestMessage(method, url);

            if (data != null)
                request.Content = JsonContent.Create(data);

            return await _apiService.SendAsync(request);
        }

        private static async Task<string> ReadError(
            HttpResponseMessage response)
        {
            string message = await response.Content.ReadAsStringAsync();

            return string.IsNullOrWhiteSpace(message)
                ? "The API could not complete the request."
                : message;
        }
    }
}