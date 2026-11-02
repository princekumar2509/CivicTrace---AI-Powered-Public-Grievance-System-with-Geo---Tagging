using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PublicGrievanceSystem.Models
{
    [Index(nameof(ComplaintId), nameof(UserId), IsUnique = true)]
    public class ComplaintUpvote
    {
        public int Id { get; set; }

        [Required]
        public int ComplaintId { get; set; }

        public Complaint Complaint { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}