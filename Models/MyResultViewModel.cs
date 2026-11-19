namespace RaceDayMVC.Models
{
    public class MyResultViewModel
    {
        public int ResultsID { get; set; }

        public int EnrollmentID { get; set; }

        public int EventID { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        public string ResultStatus { get; set; } = string.Empty;

        // Additional information for displaying results in MVC.
        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string CategoryName { get; set; } = string.Empty;
    }
}