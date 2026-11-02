using System;
using System.ComponentModel.DataAnnotations;

namespace PublicGrievanceSystem.Models
{
    public class ComplaintResolutionEvidence
    {
        public int Id { get; set; }

        public int ComplaintId { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public string? UploadedByUserId { get; set; }

        public Complaint? Complaint { get; set; }
    }
}