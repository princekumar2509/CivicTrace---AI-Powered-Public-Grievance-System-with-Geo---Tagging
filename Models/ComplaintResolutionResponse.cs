using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PublicGrievanceSystem.Models
{
    [Index(nameof(ComplaintId), nameof(UserId), IsUnique = true)]
    public class ComplaintResolutionResponse
    {
        public int Id { get; set; }

        [Required]
        public int ComplaintId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Confirmed or NotResolved
        [Required]
        public string Decision { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public DateTime RespondedAt { get; set; } = DateTime.Now;
        // Admin review status: Pending, Reopened, Rejected or Confirmed.
public string? ReviewStatus { get; set; } = "Pending";

// Explanation provided by the administrator.
[MaxLength(1000)]
public string? AdminRemarks { get; set; }

// Administrator who reviewed the citizen's objection.
public string? ReviewedByUserId { get; set; }

// Date and time of administrator review.
public DateTime? ReviewedAt { get; set; }

        public Complaint? Complaint { get; set; }
    }
}
