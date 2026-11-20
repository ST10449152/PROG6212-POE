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
    public class OrganiserEnrolmentsController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public OrganiserEnrolmentsController(
            RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // Display enrolments and results for one event.
        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            var enrolments = new List<OrganiserEnrolmentViewModel>();

            var eventCheck = await GetOwnedEvent(eventId);

            if (eventCheck.Event == null)
                return eventCheck.Error!;

            ViewBag.EventName = eventCheck.Event.EventName;
            ViewBag.EventID = eventId;

            try
            {
                using var enrolmentResponse = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/enrolments");

                if (!enrolmentResponse.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        await ReadError(enrolmentResponse);
                    return View(enrolments);
                }

                enrolments = await enrolmentResponse.Content
                    .ReadFromJsonAsync<List<OrganiserEnrolmentViewModel>>()
                    ?? new List<OrganiserEnrolmentViewModel>();

                // Retrieve category names.
                using var categoryResponse = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/categories");

                if (categoryResponse.IsSuccessStatusCode)
                {
                    var categories = await categoryResponse.Content
                        .ReadFromJsonAsync<List<CategoryViewModel>>();

                    if (categories != null)
                    {
                        foreach (var enrolment in enrolments)
                        {
                            var category = categories.FirstOrDefault(
                                c => c.CategoryID == enrolment.CategoryID);

                            enrolment.CategoryName =
                                category?.CategoryName
                                ?? $"Category {enrolment.CategoryID}";
                        }
                    }
                }

                // Retrieve results already recorded by the organiser.
                using var resultsResponse = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/results");

                if (resultsResponse.IsSuccessStatusCode)
                {
                    var results = await resultsResponse.Content
                        .ReadFromJsonAsync<List<EventResultData>>();

                    if (results != null)
                    {
                        foreach (var enrolment in enrolments)
                        {
                            var result = results.FirstOrDefault(
                                r => r.EnrollmentID == enrolment.EnrollmentID);

                            if (result != null)
                            {
                                enrolment.ResultsID = result.ResultsID;
                                enrolment.FinishTime = result.FinishTime;
                                enrolment.Position = result.Position;
                                enrolment.ResultStatus = result.ResultStatus;
                            }
                        }
                    }
                }
                else
                {
                    ViewBag.ErrorMessage =
                        "Enrolments loaded, but race results could not be loaded.";
                }
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                ViewBag.ErrorMessage =
                    "Unable to load enrolments or results from RaceDayAPI.";
            }

            return View(enrolments);
        }

        // Display the form for recording a result.
        [HttpGet]
        public async Task<IActionResult> CreateResult(
            int eventId, int enrollmentId)
        {
            var error = await CheckEnrolment(eventId, enrollmentId);

            if (error != null)
                return error;

            return View("ResultForm", new RaceResultFormViewModel
            {
                EventID = eventId,
                EnrollmentID = enrollmentId,
                ResultStatus = "Finished"
            });
        }

        // Send the new result to RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResult(
            RaceResultFormViewModel model)
        {
            ValidateResult(model);

            if (!ModelState.IsValid)
                return View("ResultForm", model);

            var error = await CheckEnrolment(
                model.EventID, model.EnrollmentID);

            if (error != null)
                return error;

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Post,
                    $"api/enrolments/{model.EnrollmentID}/result",
                    CreatePayload(model));

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Race result recorded successfully.";

                    return RedirectToAction(nameof(Index),
                        new { eventId = model.EventID });
                }

                ModelState.AddModelError(
                    "", await ReadError(response));
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "Unable to connect to RaceDayAPI.");
            }

            return View("ResultForm", model);
        }

        // Display the form for editing a result.
        [HttpGet]
        public async Task<IActionResult> EditResult(
            int eventId, int id)
        {
            var eventCheck = await GetOwnedEvent(eventId);

            if (eventCheck.Event == null)
                return eventCheck.Error!;

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/results");

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502, await ReadError(response));

                var results = await response.Content
                    .ReadFromJsonAsync<List<EventResultData>>();

                var result = results?.FirstOrDefault(
                    r => r.ResultsID == id);

                if (result == null)
                    return NotFound("Result not found.");

                return View("ResultForm", new RaceResultFormViewModel
                {
                    ResultsID = result.ResultsID,
                    EnrollmentID = result.EnrollmentID,
                    EventID = eventId,
                    FinishTime = result.FinishTime,
                    Position = result.Position,
                    ResultStatus = result.ResultStatus
                });
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                return StatusCode(502,
                    "Unable to load the race result.");
            }
        }

        // Update an existing result.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResult(
            RaceResultFormViewModel model)
        {
            ValidateResult(model);

            if (!ModelState.IsValid)
                return View("ResultForm", model);

            var eventCheck = await GetOwnedEvent(model.EventID);

            if (eventCheck.Event == null)
                return eventCheck.Error!;

            try
            {
                // Verify the result belongs to this event.
                using var checkResponse = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{model.EventID}/results");

                if (!checkResponse.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to verify the race result.");

                var results = await checkResponse.Content
                    .ReadFromJsonAsync<List<EventResultData>>();

                bool resultExists = results?.Any(r =>
                    r.ResultsID == model.ResultsID &&
                    r.EnrollmentID == model.EnrollmentID) == true;

                if (!resultExists)
                    return NotFound("Result not found for this event.");

                using var response = await SendRequest(
                    HttpMethod.Put,
                    $"api/results/{model.ResultsID}",
                    CreatePayload(model));

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Race result updated successfully.";

                    return RedirectToAction(nameof(Index),
                        new { eventId = model.EventID });
                }

                ModelState.AddModelError(
                    "", await ReadError(response));
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is TaskCanceledException)
            {
                ModelState.AddModelError(
                    "", "Unable to update the race result.");
            }

            return View("ResultForm", model);
        }

        // Validate the finishing information.
        private void ValidateResult(RaceResultFormViewModel model)
        {
            if (model.ResultStatus != "Finished" &&
                model.ResultStatus != "Disqualified" &&
                model.ResultStatus != "Did Not Finish")
            {
                ModelState.AddModelError(
                    nameof(model.ResultStatus),
                    "Select a valid result status.");
            }

            if (model.ResultStatus == "Finished")
            {
                if (model.FinishTime == null ||
                    model.FinishTime <= TimeSpan.Zero)
                {
                    ModelState.AddModelError(
                        nameof(model.FinishTime),
                        "Enter a valid finishing time.");
                }

                if (model.Position == null)
                {
                    ModelState.AddModelError(
                        nameof(model.Position),
                        "Enter the finishing position.");
                }
            }
            else
            {
                model.FinishTime = null;
                model.Position = null;
            }
        }

        // Only send fields expected by the API DTO.
        private static object CreatePayload(
            RaceResultFormViewModel model)
        {
            return new
            {
                model.FinishTime,
                model.Position,
                model.ResultStatus
            };
        }

        // Verify that the organiser owns this event.
        private async Task<(EventViewModel? Event,
            IActionResult? Error)> GetOwnedEvent(int eventId)
        {
            int? organiserId = HttpContext.Session.GetInt32("UserID");

            if (organiserId == null)
                return (null, RedirectToAction("Login", "Account"));

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get, "api/events");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return (null, RedirectToAction("Login", "Account"));

                if (!response.IsSuccessStatusCode)
                    return (null, StatusCode(502,
                        "Unable to retrieve events."));

                var events = await response.Content
                    .ReadFromJsonAsync<List<EventViewModel>>();

                var selectedEvent = events?.FirstOrDefault(
                    e => e.EventID == eventId);

                if (selectedEvent == null)
                    return (null, NotFound("Event not found."));

                if (selectedEvent.OrganiserID != organiserId.Value)
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

        // Check that the enrolment belongs to this event.
        private async Task<IActionResult?> CheckEnrolment(
            int eventId, int enrollmentId)
        {
            var eventCheck = await GetOwnedEvent(eventId);

            if (eventCheck.Event == null)
                return eventCheck.Error;

            try
            {
                using var response = await SendRequest(
                    HttpMethod.Get,
                    $"api/events/{eventId}/enrolments");

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502,
                        "Unable to verify the enrolment.");

                var enrolments = await response.Content
                    .ReadFromJsonAsync<List<OrganiserEnrolmentViewModel>>();

                bool exists = enrolments?.Any(e =>
                    e.EnrollmentID == enrollmentId &&
                    e.EventID == eventId) == true;

                if (!exists)
                    return NotFound("Enrolment not found.");

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
            var message = await response.Content.ReadAsStringAsync();

            return string.IsNullOrWhiteSpace(message)
                ? "The API could not complete the request."
                : message;
        }

        // Matches GET api/events/{eventId}/results.
        private class EventResultData
        {
            public int ResultsID { get; set; }
            public int EnrollmentID { get; set; }
            public int ParticipantID { get; set; }
            public TimeSpan? FinishTime { get; set; }
            public int? Position { get; set; }
            public string ResultStatus { get; set; } = string.Empty;
        }
    }
}