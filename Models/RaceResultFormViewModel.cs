using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class RaceResultFormViewModel
    {
        public int ResultsID { get; set; }
        public int EnrollmentID { get; set; }
        public int EventID { get; set; }

        [Display(Name = "Finish Time (HH:MM:SS)")]
        public TimeSpan? FinishTime { get; set; }

        [Range(1, 100000, ErrorMessage = "Position must be greater than zero.")]
        public int? Position { get; set; }

        [Required]
        [Display(Name = "Result Status")]
        public string ResultStatus { get; set; } = "Finished";
    }
}