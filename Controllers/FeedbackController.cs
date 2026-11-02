
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Data;
using PublicGrievanceSystem.Models;

namespace PublicGrievanceSystem.Controllers
{
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeedbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show the feedback form for a resolved complaint.
        [HttpGet]
        public async Task<IActionResult> Create(int complaintId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }
            // Check whether the administrator has enabled citizen feedback.
var settings = await _context.ComplaintUploadSettings
    .AsNoTracking()
    .FirstOrDefaultAsync() ?? new ComplaintUploadSettings();

if (!settings.EnableCitizenFeedback)
{
    TempData["ErrorMessage"] =
        "Citizen feedback is currently disabled by the administrator.";

    return RedirectToAction("Index", "Citizen");
}

            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (complaint == null)
            {
                return NotFound();
            }

            // A citizen can only access their own complaint.
            if (complaint.UserId != userId)
            {
                return Forbid();
            }

            if (!string.Equals(
                    complaint.Status,
                    "Resolved",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Feedback is available only for resolved complaints.";

                return RedirectToAction("Index", "Citizen");
            }

            var alreadySubmitted = await _context.ComplaintFeedbacks
                .AnyAsync(f => f.ComplaintId == complaintId);

            if (alreadySubmitted)
            {
                TempData["ErrorMessage"] =
                    "Feedback has already been submitted for this complaint.";

                return RedirectToAction("Index", "Citizen");
            }

            var model = new FeedbackFormViewModel
            {
                ComplaintId = complaint.Id,
                ComplaintTitle = complaint.Title
            };

            return View(model);
        }

        // Save feedback after verifying ownership and resolution status again.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeedbackFormViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }
            // Check whether the administrator has enabled citizen feedback.
var settings = await _context.ComplaintUploadSettings
    .AsNoTracking()
    .FirstOrDefaultAsync() ?? new ComplaintUploadSettings();

if (!settings.EnableCitizenFeedback)
{
    TempData["ErrorMessage"] =
        "Citizen feedback is currently disabled by the administrator.";

    return RedirectToAction("Index", "Citizen");
}

            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == model.ComplaintId);

            if (complaint == null)
            {
                return NotFound();
            }

            if (complaint.UserId != userId)
            {
                return Forbid();
            }

            if (!string.Equals(
                    complaint.Status,
                    "Resolved",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Feedback is available only for resolved complaints.";

                return RedirectToAction("Index", "Citizen");
            }

            var alreadySubmitted = await _context.ComplaintFeedbacks
                .AnyAsync(f => f.ComplaintId == complaint.Id);

            if (alreadySubmitted)
            {
                TempData["ErrorMessage"] =
                    "Feedback has already been submitted for this complaint.";

                return RedirectToAction("Index", "Citizen");
            }

            if (!ModelState.IsValid)
            {
                model.ComplaintTitle = complaint.Title;
                return View(model);
            }

            var feedback = new ComplaintFeedback
            {
                ComplaintId = complaint.Id,
                UserId = userId,
                Rating = model.Rating,
                Comments = string.IsNullOrWhiteSpace(model.Comments)
                    ? null
                    : model.Comments.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            _context.ComplaintFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Thank you! Your feedback has been submitted.";

            return RedirectToAction("Index", "Citizen");
        }
    }
}
