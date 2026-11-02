
using System;
using System.ComponentModel.DataAnnotations;

namespace PublicGrievanceSystem.Models
{
    public class ComplaintEvidence
    {
        public int Id { get; set; }

        public int ComplaintId { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public Complaint? Complaint { get; set; }
    }
}
