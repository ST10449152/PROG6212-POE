namespace RaceDayMVC.Models
{
    // Combines event information and its categories.
    public class EventDetailsViewModel
    {
        public EventViewModel Event { get; set; } = new EventViewModel();

        public List<CategoryViewModel> Categories { get; set; }
            = new List<CategoryViewModel>();

        public string RouteDescription { get; set; } = string.Empty;

        public string RouteMapURL { get; set; } = string.Empty;
    }
}