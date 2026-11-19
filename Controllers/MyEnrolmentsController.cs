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
    public class MyEnrolmentsController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public MyEnrolmentsController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /MyEnrolments
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Only participant accounts can view their enrolments.
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return StatusCode(403,
                    "Only participants can view personal enrolments.");
            }

            List<MyEnrolmentViewModel> enrolments =
                new List<MyEnrolmentViewModel>();

            try
            {
                // Get the logged-in participant's enrolments.
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "api/enrolments/me");

                using var response =
                    await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage =
                        "Unable to load your enrolments.";

                    return View(enrolments);
                }

                enrolments = await response.Content
                    .ReadFromJsonAsync<List<MyEnrolmentViewModel>>()
                    ?? new List<MyEnrolmentViewModel>();

                // Get additional details for each enrolment.
                foreach (var enrolment in enrolments)
                {
                    using var eventRequest = new HttpRequestMessage(
                        HttpMethod.Get,
                        $"api/events/{enrolment.EventID}");

                    using var eventResponse =
                        await _apiService.SendAsync(eventRequest);

                    if (eventResponse.StatusCode ==
                        HttpStatusCode.Unauthorized)
                    {
                        HttpContext.Session.Clear();
                        return RedirectToAction("Login", "Account");
                    }

                    if (eventResponse.IsSuccessStatusCode)
                    {
                        var race = await eventResponse.Content
                            .ReadFromJsonAsync<EventViewModel>();

                        if (race != null)
                        {
                            enrolment.EventName = race.EventName;
                            enrolment.EventDate = race.EventDate;
                            enrolment.Location = race.Location;
                        }
                    }

                    // Get the category name for this enrolment.
                    using var categoryRequest = new HttpRequestMessage(
                        HttpMethod.Get,
                        $"api/events/{enrolment.EventID}/categories");

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
                        var categories = await categoryResponse.Content
                            .ReadFromJsonAsync<List<CategoryViewModel>>();

                        var selectedCategory = categories?
                            .FirstOrDefault(c =>
                                c.CategoryID == enrolment.CategoryID);

                        if (selectedCategory != null)
                        {
                            enrolment.CategoryName =
                                selectedCategory.CategoryName;
                        }
                    }
                }

                enrolments = enrolments
                    .OrderByDescending(e => e.EnrollmentDate)
                    .ToList();
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage =
                    "Unable to connect to RaceDayAPI.";
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage =
                    "Unable to read your enrolment information.";
            }

            return View(enrolments);
        }
    }
}