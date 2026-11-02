using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace PublicGrievanceSystem.Models
{
    public class Complaint
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }

        [Required]
        [Display(Name = "Complaint Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        //[Required]
        public string Category { get; set; } = string.Empty;

        public string Priority { get; set; } = "Medium";

        //AI Sverity Prediction: Low, Medium, High
        public string AISeverity { get; set; } = "Medium";

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? Address { get; set; }

        public string? PinCode { get; set; }

        public string? AIImageCategory { get; set; }

        public double? AIImageConfidence { get; set; }

        public string? AIImageDescription { get; set; }

        public string? ImagePath { get; set; }

        public string Status { get; set; } = "Submitted";

        public string? Department { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

// Resolution & Escalation Tracking

public DateTime? ResolutionDeadline { get; set; }
public DateTime? OverdueAt { get; set; }

public DateTime? EscalatedAt { get; set; }

public DateTime? ResolvedAt { get; set; }
        public ICollection<ComplaintEvidence> Evidences { get; set; }
    = new List<ComplaintEvidence>();
    }
    
}