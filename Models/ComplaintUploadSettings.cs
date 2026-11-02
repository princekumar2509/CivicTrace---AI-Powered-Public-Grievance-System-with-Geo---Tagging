
using System.ComponentModel.DataAnnotations;

namespace PublicGrievanceSystem.Models
{
    public class ComplaintUploadSettings
    {
        public int Id { get; set; }

        [Range(1, 20)]
        public int MaximumImageSizeMb { get; set; } = 5;

        [Required]
        public string AllowedImageFormats { get; set; } =
            "jpg,jpeg,png,webp";

        public bool RequireImage { get; set; } = false;

        public bool AllowMultipleImages { get; set; } = false;

        [Range(1, 10)]
        public int MaximumImagesPerComplaint { get; set; } = 3;
        
        // Complaint workflow settings

        public bool AllowNewComplaints { get; set; } = true;

        public bool AllowCitizensToReopenComplaints { get; set; } = false;

        public bool RequireComplaintDescription { get; set; } = true;

        public bool RequireLocation { get; set; } = true;

        public bool AllowAnonymousComplaints { get; set; } = false;

        public bool EnableDuplicateDetection { get; set; } = true;

        public bool EnableCitizenFeedback { get; set; } = true;
        // Resolution & Escalation Settings

[Range(1, 720)]
public int HighPriorityResolutionHours { get; set; } = 24;

[Range(1, 720)]
public int MediumPriorityResolutionHours { get; set; } = 72;

[Range(1, 720)]
public int LowPriorityResolutionHours { get; set; } = 168;

public bool EnableAutomaticOverdueDetection { get; set; } = true;

public bool EnableEscalationNotifications { get; set; } = true;

[Range(1, 720)]
public int EscalationAfterDeadlineHours { get; set; } = 24;

public bool EnableResolutionProof { get; set; } = true;

    }
}
