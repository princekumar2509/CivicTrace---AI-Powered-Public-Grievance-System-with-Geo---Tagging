using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Models;

namespace PublicGrievanceSystem.Data
{

    public class ApplicationDbContext : IdentityDbContext

    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<ComplaintUploadSettings> ComplaintUploadSettings { get; set; }
        public DbSet<ComplaintEvidence> ComplaintEvidences { get; set; }
        public DbSet<ComplaintStatusHistory> ComplaintStatusHistories { get; set; }
        public DbSet<ComplaintFeedback> ComplaintFeedbacks { get; set; }
        public DbSet<ComplaintUpvote> ComplaintUpvotes { get; set; }
        public DbSet<ComplaintResolutionEvidence> ComplaintResolutionEvidences { get; set; }
        public DbSet<ComplaintResolutionResponse> ComplaintResolutionResponses { get; set; }
    }
}
