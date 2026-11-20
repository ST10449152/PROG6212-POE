namespace RaceDayMVC.Models
{
    public class OrganiserEnrolmentViewModel
    {
        public int EnrollmentID { get; set; }
        public int ParticipantID { get; set; }
        public int EventID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public int? ResultsID { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string? ResultStatus { get; set; }
    }
}