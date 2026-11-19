namespace RaceDayMVC.Models
{
    // Stores category information received from RaceDayAPI.
    public class CategoryViewModel
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal DistanceKM { get; set; }

        public int MaxParticipants { get; set; }

        public decimal EntranceFee { get; set; }
    }
}