using System.ComponentModel.DataAnnotations;

namespace PublicGrievanceSystem.Models
{
    public class ComplaintStatusHistory
    {
        public int Id { get; set; }

        public int ComplaintId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Department { get; set; }

        public string? Priority { get; set; }

        public DateTime ChangedAt { get; set; }

        public string? Remarks { get; set; }
    }
}