using System;

namespace RaceDayMVC.Models
{
    // Stores event information received from RaceDayAPI.
    public class EventViewModel
    {
        public int EventID { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public decimal DistanceKM { get; set; }

        public string EventType { get; set; } = string.Empty;

        public int OrganiserID { get; set; }

        public int VenueID { get; set; }
    }
}