
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PublicGrievanceSystem.Models
{
    [Index(nameof(ComplaintId), IsUnique = true)]
    public class ComplaintFeedback
    {
        public int Id { get; set; }

        [Required]
        public int ComplaintId { get; set; }

        [ForeignKey(nameof(ComplaintId))]
        public Complaint Complaint { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please select a rating from 1 to 5.")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters.")]
        public string? Comments { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
