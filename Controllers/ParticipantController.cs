using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Filters;

namespace RaceDayMVC.Controllers
{
    // Participants and Organisers can access this interface.
    [SessionAuthorize("Participant")]
    public class ParticipantController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}