using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Services;
using System.Net;
using System.Net.Http.Headers;

namespace RaceDayMVC.Controllers
{
    public class ImageController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public ImageController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        // Upload an event banner through RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadEventBanner(
            int eventId, IFormFile? image)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("Role") != "Organiser")
                return StatusCode(403,
                    "Only organisers can upload event banners.");

            if (eventId <= 0)
                return BadRequest("Invalid event.");

            if (!IsValidImage(image))
            {
                TempData["ErrorMessage"] =
                    "Select a JPG, PNG or WEBP image smaller than 5 MB.";

                return RedirectToAction("Index", "Organiser");
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/images/events/{eventId}/banner");

                using var form = new MultipartFormDataContent();
                using var stream = image!.OpenReadStream();
                using var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(GetContentType(image.FileName));

                form.Add(fileContent, "image", image.FileName);
                request.Content = form;

                using var response =
                    await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event banner uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        await ReadError(response);
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
                    "The image upload timed out.";
            }

            return RedirectToAction("Index", "Organiser");
        }

        // Upload a profile picture through RaceDayAPI.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile? image)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login", "Account");

            if (!IsValidImage(image))
            {
                TempData["ErrorMessage"] =
                    "Select a JPG, PNG or WEBP image smaller than 5 MB.";

                return RedirectToAction("Index", "Profile");
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post, "api/images/profile");

                using var form = new MultipartFormDataContent();
                using var stream = image!.OpenReadStream();
                using var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(GetContentType(image.FileName));

                form.Add(fileContent, "image", image.FileName);
                request.Content = form;

                using var response =
                    await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return SessionExpired();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Profile picture uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        await ReadError(response);
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
                    "The image upload timed out.";
            }

            return RedirectToAction("Index", "Profile");
        }

        // Display a stored event banner through MVC.
        [HttpGet]
        public async Task<IActionResult> EventBanner(int eventId)
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized();

            if (eventId <= 0)
                return BadRequest();

            return await GetImage(
                $"api/images/events/{eventId}/banner");
        }

        // Display the logged-in user's profile picture.
        [HttpGet]
        public async Task<IActionResult> ProfilePicture()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return Unauthorized();

            return await GetImage("api/images/profile");
        }

        private async Task<IActionResult> GetImage(string apiUrl)
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, apiUrl);

                using var response =
                    await _apiService.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);

                string contentType =
                    response.Content.Headers.ContentType?.MediaType
                    ?? "application/octet-stream";

                // Return the image to the browser without exposing
                // the private API session cookie.
                byte[] imageBytes =
                    await response.Content.ReadAsByteArrayAsync();

                Response.Headers.CacheControl = "no-store";

                return File(imageBytes, contentType);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504);
            }
        }

        private static bool IsValidImage(IFormFile? image)
        {
            if (image == null ||
                image.Length == 0 ||
                image.Length > 5 * 1024 * 1024)
                return false;

            string extension =
                Path.GetExtension(image.FileName).ToLowerInvariant();

            return extension == ".jpg" ||
                   extension == ".jpeg" ||
                   extension == ".png" ||
                   extension == ".webp";
        }

        private static string GetContentType(string fileName)
        {
            string extension =
                Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        private static async Task<string> ReadError(
            HttpResponseMessage response)
        {
            string message =
                await response.Content.ReadAsStringAsync();

            return string.IsNullOrWhiteSpace(message)
                ? "Unable to upload the image."
                : message;
        }

        private IActionResult SessionExpired()
        {
            HttpContext.Session.Clear();

            TempData["ErrorMessage"] =
                "Your session has expired. Please log in again.";

            return RedirectToAction("Login", "Account");
        }
    }
}