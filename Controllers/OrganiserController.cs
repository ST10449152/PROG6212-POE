using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;

namespace RaceDayMVC.Controllers
{
    // Only Organisers can access this dashboard.
    [SessionAuthorize("Organiser")]
    public class OrganiserController : Controller
    {
        // GET: /Organiser
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}