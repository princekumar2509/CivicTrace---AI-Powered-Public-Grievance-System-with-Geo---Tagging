using System;

namespace PublicGrievanceSystem.Models
{
    public class CommunityComplaintViewModel
    {
        public int ComplaintId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; }

        public int UpvoteCount { get; set; }

        public bool HasUpvoted { get; set; }
    }
}
