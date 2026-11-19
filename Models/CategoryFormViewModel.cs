using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models
{
    public class CategoryFormViewModel
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.01, 9999.99)]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKM { get; set; }

        [Range(1, 100000)]
        [Display(Name = "Maximum Participants")]
        public int MaxParticipants { get; set; }

        [Range(0, 999999)]
        [Display(Name = "Entrance Fee (R)")]
        public decimal EntranceFee { get; set; }
    }
}