using System.Net.Http.Headers;

namespace RaceDayMVC.Services
{
    public class RaceDayApiService
    {
        private readonly HttpClient _api;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RaceDayApiService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _api = httpClientFactory.CreateClient("RaceDayAPI");
            _httpContextAccessor = httpContextAccessor;
        }

        // Send a request with the logged-in user's API cookie.
        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request)
        {
            var context = _httpContextAccessor.HttpContext;

            if (context == null)
            {
                throw new InvalidOperationException(
                    "HTTP context is not available.");
            }

            // Read the API cookie from the MVC session.
            string? sessionCookie = context.Session.GetString(
                "ApiSessionCookie");

            if (!string.IsNullOrEmpty(sessionCookie))
            {
                // Forward only this user's API session cookie.
                request.Headers.TryAddWithoutValidation(
                    "Cookie", sessionCookie);
            }

            return await _api.SendAsync(request);
        }
    }
}