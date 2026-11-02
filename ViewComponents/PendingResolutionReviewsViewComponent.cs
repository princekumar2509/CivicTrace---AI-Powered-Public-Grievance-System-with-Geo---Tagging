using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Data;
using PublicGrievanceSystem.ViewModels;
using System;
using System.Threading.Tasks;

namespace PublicGrievanceSystem.ViewComponents
{
    public class PendingResolutionReviewsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public PendingResolutionReviewsViewComponent(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentTime = DateTime.Now;

            var pendingCount =
                await _context.ComplaintResolutionResponses
                    .AsNoTracking()
                    .CountAsync(r =>
                        r.Decision == "NotResolved" &&
                        r.ReviewStatus == "Pending");

            var overdueCount =
                await _context.Complaints
                    .AsNoTracking()
                    .CountAsync(c =>
                        c.ResolutionDeadline.HasValue &&
                        c.ResolutionDeadline.Value <= currentTime &&
                        c.Status != "Resolved" &&
                        !c.EscalatedAt.HasValue);

            var escalatedCount =
                await _context.Complaints
                    .AsNoTracking()
                    .CountAsync(c =>
                        c.EscalatedAt.HasValue &&
                        c.Status != "Resolved");

            var model = new AdminQuickMenuViewModel
            {
                PendingResolutionReviewsCount = pendingCount,
                OverdueComplaintsCount = overdueCount,
                EscalatedComplaintsCount = escalatedCount
            };

            return View(model);
        }
    }
}
