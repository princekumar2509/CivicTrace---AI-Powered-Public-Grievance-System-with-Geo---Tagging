using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Data;
using PublicGrievanceSystem.Models;
using System.Security.Claims;

namespace PublicGrievanceSystem.Controllers
{
    [Authorize(Roles = "Citizen")]
    public class CitizenController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CitizenController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // CITIZEN DASHBOARD
        // =========================
        public async Task<IActionResult> Index()
{
    var currentUserId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(currentUserId))
    {
        return Challenge();
    }

    var complaints = await _context.Complaints
        .Where(c => c.UserId == currentUserId)
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync();


    // ==========================================
    // RECENT STATUS UPDATES
    // ==========================================

    var complaintIds = complaints
        .Select(c => c.Id)
        .ToList();

        // ==========================================
// COMPLAINT FEEDBACK STATUS
// ==========================================

var feedbackSubmittedIds = await _context.ComplaintFeedbacks
    .Where(f => complaintIds.Contains(f.ComplaintId)
                && f.UserId == currentUserId)
    .Select(f => f.ComplaintId)
    .ToListAsync();

ViewBag.FeedbackSubmittedIds = feedbackSubmittedIds.ToHashSet();


var allUpdates =
    await _context.ComplaintStatusHistories
        .Where(h => complaintIds.Contains(h.ComplaintId))
        .OrderByDescending(h => h.ChangedAt)
        .ThenByDescending(h => h.Id)
        .ToListAsync();

var recentUpdates = allUpdates
    .GroupBy(h => h.ComplaintId)
    .Select(g => g.First())
    .OrderByDescending(h => h.ChangedAt)
    .ThenByDescending(h => h.Id)
    .Take(1)
    .ToList();

ViewBag.RecentUpdates = recentUpdates;
var resolutionReviewUpdates = await _context.ComplaintResolutionResponses
    .AsNoTracking()
    .Include(r => r.Complaint)
    .Where(r =>
        r.UserId == currentUserId &&
        (r.ReviewStatus == "Reopened" ||
         r.ReviewStatus == "Rejected"))
    .OrderByDescending(r => r.ReviewedAt)
    .Take(5)
    .ToListAsync();

ViewBag.ResolutionReviewUpdates = resolutionReviewUpdates;


    return View(complaints);
}
// ============================================
// COMMUNITY COMPLAINTS
// ============================================
[HttpGet]
public async Task<IActionResult> Community()
{
    var currentUserId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(currentUserId))
    {
        return Challenge();
    }

    // Get the total upvotes for each complaint.
    var upvoteCounts = await _context.ComplaintUpvotes
        .AsNoTracking()
        .GroupBy(u => u.ComplaintId)
        .Select(g => new
        {
            ComplaintId = g.Key,
            Count = g.Count()
        })
        .ToDictionaryAsync(
            x => x.ComplaintId,
            x => x.Count);

    // Get complaints already supported by this citizen.
    var myUpvotedIds = await _context.ComplaintUpvotes
        .AsNoTracking()
        .Where(u => u.UserId == currentUserId)
        .Select(u => u.ComplaintId)
        .ToListAsync();

    var myUpvotedSet = myUpvotedIds.ToHashSet();

    // Retrieve public complaint information.
    var communityComplaints = await _context.Complaints
        .AsNoTracking()
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new CommunityComplaintViewModel
        {
            ComplaintId = c.Id,
            Title = c.Title,
            Description = c.Description,
            Category = c.Category,
            Priority = c.Priority,
            Status = c.Status,
            Address = c.Address,
            CreatedAt = c.CreatedAt
        })
        .ToListAsync();

    // Attach upvote counts and citizen voting status.
    foreach (var complaint in communityComplaints)
    {
        complaint.UpvoteCount =
            upvoteCounts.TryGetValue(
                complaint.ComplaintId,
                out var count) ? count : 0;

        complaint.HasUpvoted =
            myUpvotedSet.Contains(complaint.ComplaintId);
    }

    return View(communityComplaints);
}
// =========================
// COMPLAINT DETAILS
// =========================
public async Task<IActionResult> Details(int id)
{
    if (id <= 0)
    {
        return NotFound();
    }

    var currentUserId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(currentUserId))
    {
        return Challenge();
    }

    var complaint = await _context.Complaints
    .Include(c => c.Evidences)
    .FirstOrDefaultAsync(c =>
        c.Id == id &&
        c.UserId == currentUserId);

    if (complaint == null)
    {
        return NotFound();
    }

    var statusHistory =
        await _context.ComplaintStatusHistories
            .Where(h => h.ComplaintId == complaint.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
// Check whether citizen reopening is enabled in Admin settings.
var reopenSettings = await _context.ComplaintUploadSettings
    .AsNoTracking()
    .FirstOrDefaultAsync();

ViewBag.AllowCitizenReopen =
    reopenSettings?.AllowCitizensToReopenComplaints ?? false;
    // Retrieve resolution photographs for this complaint.
var resolutionEvidence = await _context.ComplaintResolutionEvidences
    .AsNoTracking()
    .Where(e => e.ComplaintId == complaint.Id)
    .OrderByDescending(e => e.UploadedAt)
    .ToListAsync();

ViewBag.ResolutionEvidence = resolutionEvidence;
// Retrieve this citizen's previous resolution response.
var resolutionResponse = await _context.ComplaintResolutionResponses
    .AsNoTracking()
    .FirstOrDefaultAsync(r =>
        r.ComplaintId == complaint.Id &&
        r.UserId == currentUserId);

ViewBag.ResolutionResponse = resolutionResponse;
    ViewBag.StatusHistory = statusHistory;

    return View(complaint);
}
// ============================================
// COMMUNITY COMPLAINT UPVOTE
// ============================================
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Upvote(int id)
{
    var currentUserId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(currentUserId))
    {
        return Challenge();
    }

    if (id <= 0)
    {
        return NotFound();
    }

    // Check whether the complaint exists.
    var complaint = await _context.Complaints
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == id);

    if (complaint == null)
    {
        return NotFound();
    }

    // Citizens cannot upvote their own complaints.
    if (complaint.UserId == currentUserId)
    {
        TempData["CommunityMessage"] =
            "You cannot upvote your own complaint.";

        return RedirectToAction(nameof(Community));
    }

    // Check whether this citizen has already upvoted.
    var existingUpvote = await _context.ComplaintUpvotes
        .FirstOrDefaultAsync(u =>
            u.ComplaintId == id &&
            u.UserId == currentUserId);

    if (existingUpvote != null)
    {
        // Remove existing support.
        _context.ComplaintUpvotes.Remove(existingUpvote);

        await _context.SaveChangesAsync();

        TempData["CommunityMessage"] =
            "Your support has been removed.";
    }
    else
    {
        // Add new support.
        var newUpvote = new ComplaintUpvote
        {
            ComplaintId = id,
            UserId = currentUserId,
            CreatedAt = DateTime.Now
        };

        _context.ComplaintUpvotes.Add(newUpvote);

        try
        {
            await _context.SaveChangesAsync();

            TempData["CommunityMessage"] =
                "Thank you! Your support has been added.";
        }
        catch (DbUpdateException)
        {
            TempData["CommunityMessage"] =
                "Your support could not be added. Please refresh and try again.";
        }
    }

    return RedirectToAction(nameof(Community));
}
// ============================================
// CITIZEN RESOLUTION VERIFICATION
// ============================================
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Citizen")]
public async Task<IActionResult> SubmitResolutionResponse(
    int id,
    string decision,
    string? remarks)
{
    var currentUserId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(currentUserId))
    {
        return Challenge();
    }

    // Retrieve only the complaint belonging to this citizen.
    var complaint = await _context.Complaints
        .FirstOrDefaultAsync(c =>
            c.Id == id &&
            c.UserId == currentUserId);

    if (complaint == null)
    {
        return NotFound();
    }

    // Only resolved complaints can be verified.
    if (complaint.Status != "Resolved")
    {
        TempData["ResolutionResponseError"] =
            "You can respond only after the complaint is marked as Resolved.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Resolution proof must exist before citizen verification.
    var hasResolutionProof =
        await _context.ComplaintResolutionEvidences
            .AnyAsync(e => e.ComplaintId == id);

    if (!hasResolutionProof)
    {
        TempData["ResolutionResponseError"] =
            "Resolution photographs are not available yet.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Accept only the two supported decisions.
    if (decision != "Confirmed" && decision != "NotResolved")
    {
        return BadRequest("Invalid resolution response.");
    }

    if (!string.IsNullOrWhiteSpace(remarks) && remarks.Length > 1000)
    {
        TempData["ResolutionResponseError"] =
            "Your remarks cannot exceed 1000 characters.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Check whether this citizen has already responded.
    var existingResponse =
        await _context.ComplaintResolutionResponses
            .FirstOrDefaultAsync(r =>
                r.ComplaintId == id &&
                r.UserId == currentUserId);
                if (existingResponse?.Decision == "Confirmed")
{
    TempData["ResolutionResponseError"] =
        "You have already confirmed that this complaint is resolved. Further responses are not allowed.";

    return RedirectToAction(nameof(Details), new { id });
}

    var previousDecision = existingResponse?.Decision;

// Determine the review status.
var reviewStatus = decision == "NotResolved"
    ? "Pending"
    : "Confirmed";

if (existingResponse == null)
{
    // Create the citizen's first resolution response.
    existingResponse = new ComplaintResolutionResponse
    {
        ComplaintId = id,
        UserId = currentUserId,
        Decision = decision,
        Remarks = remarks?.Trim(),
        RespondedAt = DateTime.Now,
        ReviewStatus = reviewStatus
    };

    _context.ComplaintResolutionResponses.Add(existingResponse);
}
else
{
    // Update the citizen's existing response.
    existingResponse.Decision = decision;
    existingResponse.Remarks = remarks?.Trim();
    existingResponse.RespondedAt = DateTime.Now;

    // Reset the previous admin review when the citizen responds again.
    existingResponse.ReviewStatus = reviewStatus;
    existingResponse.AdminRemarks = null;
    existingResponse.ReviewedByUserId = null;
    existingResponse.ReviewedAt = null;
}

    // Record a history entry whenever the decision changes.
    if (previousDecision != decision)
    {
        var history = new ComplaintStatusHistory
        {
            ComplaintId = complaint.Id,
            Status = complaint.Status,
            Department = complaint.Department,
            Priority = complaint.Priority,
            ChangedAt = DateTime.Now,
            Remarks = decision == "Confirmed"
                ? "Citizen confirmed that the complaint has been resolved."
                : "Citizen reported that the issue is not resolved and requested further action."
        };

        _context.ComplaintStatusHistories.Add(history);
    }

    try
    {
        await _context.SaveChangesAsync();

        TempData["ResolutionResponseSuccess"] =
            decision == "Confirmed"
                ? "Thank you! You confirmed that the issue has been resolved."
                : "Your report has been recorded for administrator review.";
    }
    catch (DbUpdateException)
    {
        TempData["ResolutionResponseError"] =
            "Unable to save your response. Please try again.";
    }

    return RedirectToAction(nameof(Details), new { id });
}
        // ============================================
        // REOPEN A RESOLVED COMPLAINT
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> Reopen(int id)
        {
            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }

            // Check the administrator's reopen setting.
            var settings = await _context.ComplaintUploadSettings
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? new ComplaintUploadSettings();

            if (!settings.AllowCitizensToReopenComplaints)
            {
                TempData["ErrorMessage"] =
                    "Reopening complaints is currently disabled by the administrator.";

                return RedirectToAction(nameof(Index));
            }

            // Only retrieve a complaint belonging to this citizen.
            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UserId == currentUserId);

            if (complaint == null)
            {
                return NotFound();
            }

            // Only resolved complaints can be reopened.
            if (!string.Equals(
                    complaint.Status,
                    "Resolved",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only resolved complaints can be reopened.";

                return RedirectToAction(nameof(Index));
            }

            // Return the complaint to the authority's submission queue.
            complaint.Status = "Submitted";
            complaint.ResolvedAt = null;

            // Preserve the reopening event in the status history.
            var history = new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                Status = complaint.Status,
                Department = complaint.Department,
                Priority = complaint.Priority,
                ChangedAt = DateTime.Now,
                Remarks = "Complaint reopened by citizen."
            };

            _context.ComplaintStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Complaint #{complaint.Id} has been reopened successfully.";

            return RedirectToAction(nameof(Index));
        }

}

}
