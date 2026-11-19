using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class EventFormViewModel
    {
        public int EventID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Enter a valid Venue ID.")]
        [Display(Name = "Venue ID")]
        public int VenueID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Event Date")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; } = new(8, 0, 0);

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; } = new(12, 0, 0);

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Range(0.01, 9999.99)]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKM { get; set; }

        [Required]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = "Run";

        [Display(Name = "Route Description")]
        public string? RouteDescription { get; set; }

        [Display(Name = "Route Map URL")]
        public string? RouteMapURL { get; set; }
    }
}