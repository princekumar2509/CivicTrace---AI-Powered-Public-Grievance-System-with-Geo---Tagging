namespace PublicGrievanceSystem.ViewModels
{
    public class CitizenManagementViewModel
    {
        public string? Search { get; set; }

        public List<CitizenAccountViewModel> Citizens { get; set; }
            = new List<CitizenAccountViewModel>();
    }

    public class CitizenAccountViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public bool IsSuspended { get; set; }

        public string SuspensionReason { get; set; } = string.Empty;

    }
}
