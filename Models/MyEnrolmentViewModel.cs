namespace RaceDayMVC.Models
{
    public class MyEnrolmentViewModel
    {
        public int EnrollmentID { get; set; }
        public int EventID { get; set; }
        public int CategoryID { get; set; }

        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Extra information displayed in the MVC page.
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}