using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayMVC.Filters
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string? _requiredRole;

        public SessionAuthorizeAttribute(string? requiredRole = null)
        {
            _requiredRole = requiredRole;
        }

        public override void OnActionExecuting(
            ActionExecutingContext context)
        {
            // Read the logged-in user's session details.
            var session = context.HttpContext.Session;

            int? userId = session.GetInt32("UserID");
            string? role = session.GetString("Role");
            string? apiCookie = session.GetString("ApiSessionCookie");

            // Check whether the user is logged in.
            if (userId == null ||
                string.IsNullOrEmpty(role) ||
                string.IsNullOrEmpty(apiCookie))
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account", null);

                return;
            }

            // Check whether the requested page requires a role.
            if (!string.IsNullOrEmpty(_requiredRole))
            {
                bool allowed = false;

                if (_requiredRole == "Participant")
                {
                    // Both Participants and Organisers can use
                    // the Participant interface.
                    allowed = role == "Participant" ||
                              role == "Organiser";
                }
                else if (_requiredRole == "Organiser")
                {
                    // Only Organisers can use the Organiser interface.
                    allowed = role == "Organiser";
                }

                if (!allowed)
                {
                    context.Result = new StatusCodeResult(403);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}