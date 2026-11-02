
using System.ComponentModel.DataAnnotations;

namespace PublicGrievanceSystem.Models
{
    public class FeedbackFormViewModel
    {
        public int ComplaintId { get; set; }

        public string ComplaintTitle { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please select a rating from 1 to 5.")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters.")]
        public string? Comments { get; set; }
    }
}
