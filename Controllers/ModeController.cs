using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;

namespace RaceDayMVC.Controllers
{
    // Only Organisers can access the mode selection page.
    [SessionAuthorize("Organiser")]
    public class ModeController : Controller
    {
        // Display the mode selection page.
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Save the Organiser's selected mode.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Select(string mode)
        {
            if (mode != "Participant" && mode != "Organiser")
            {
                TempData["ErrorMessage"] =
                    "Please select a valid mode.";

                return RedirectToAction("Index");
            }

            // This changes the interface, not the user's account role.
            HttpContext.Session.SetString("SelectedMode", mode);

            if (mode == "Organiser")
            {
                return RedirectToAction("Index", "Organiser");
            }

            return RedirectToAction("Index", "Participant");
        }
    }
}