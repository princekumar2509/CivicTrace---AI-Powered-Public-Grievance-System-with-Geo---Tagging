using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Data;
using PublicGrievanceSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace PublicGrievanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
private readonly IWebHostEnvironment _environment;

public AdminController(
    ApplicationDbContext context,
    IWebHostEnvironment environment)
{
    _context = context;
    _environment = environment;
}

        // GET: /Admin
        public async Task<IActionResult> Index()
{
    var complaints = await _context.Complaints
        .OrderBy(c => c.Id)
        .ToListAsync();

    // Get the five complaints with the most upvotes.
    var topUpvoteCounts = await _context.ComplaintUpvotes
        .AsNoTracking()
        .GroupBy(u => u.ComplaintId)
        .Select(g => new
        {
            ComplaintId = g.Key,
            UpvoteCount = g.Count()
        })
        .OrderByDescending(x => x.UpvoteCount)
        .ThenBy(x => x.ComplaintId)
        .Take(5)
        .ToListAsync();

    var topComplaintIds = topUpvoteCounts
        .Select(x => x.ComplaintId)
        .ToList();

    // Retrieve the details of those complaints.
    var mostSupportedComplaints = await _context.Complaints
        .AsNoTracking()
        .Where(c => topComplaintIds.Contains(c.Id))
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

    // Attach the upvote counts.
    var upvoteCountDictionary = topUpvoteCounts
        .ToDictionary(x => x.ComplaintId, x => x.UpvoteCount);

    foreach (var complaint in mostSupportedComplaints)
    {
        complaint.UpvoteCount =
            upvoteCountDictionary[complaint.ComplaintId];
    }

    // Preserve ranking order.
    ViewBag.MostSupportedComplaints = mostSupportedComplaints
        .OrderBy(c => topComplaintIds.IndexOf(c.ComplaintId))
        .ToList();
        // ============================================
// PENDING RESOLUTION REVIEWS
// ============================================

var pendingResolutionReviews =
    await _context.ComplaintResolutionResponses
        .AsNoTracking()
        .Include(r => r.Complaint)
        .Where(r =>
            r.Decision == "NotResolved" &&
            r.ReviewStatus == "Pending")
        .OrderByDescending(r => r.RespondedAt)
        .ToListAsync();

ViewBag.PendingResolutionReviews = pendingResolutionReviews;
// ============================================
// OVERDUE COMPLAINTS
// ============================================

var currentTime = DateTime.Now;

var overdueComplaints = await _context.Complaints
    .AsNoTracking()
    .Where(c =>
        c.ResolutionDeadline.HasValue &&
        c.ResolutionDeadline.Value <= currentTime &&
        c.Status != "Resolved" &&
        !c.EscalatedAt.HasValue)
    .OrderBy(c => c.ResolutionDeadline)
    .ToListAsync();

ViewBag.OverdueComplaints = overdueComplaints;


// ============================================
// ESCALATED COMPLAINTS
// ============================================

var escalatedComplaints = await _context.Complaints
    .AsNoTracking()
    .Where(c =>
        c.EscalatedAt.HasValue &&
        c.Status != "Resolved")
    .OrderByDescending(c => c.EscalatedAt)
    .ToListAsync();

ViewBag.EscalatedComplaints = escalatedComplaints;

    return View(complaints);
}
// GET: /Admin/PendingResolutionReviews
public async Task<IActionResult> PendingResolutionReviews()
{
    var pendingResolutionReviews =
        await _context.ComplaintResolutionResponses
            .AsNoTracking()
            .Include(r => r.Complaint)
            .Where(r =>
                r.Decision == "NotResolved" &&
                r.ReviewStatus == "Pending")
            .OrderByDescending(r => r.RespondedAt)
            .ToListAsync();

    return View(pendingResolutionReviews);
}
// GET: /Admin/OverdueComplaints
public async Task<IActionResult> OverdueComplaints()
{
    var currentTime = DateTime.Now;

    var overdueComplaints = await _context.Complaints
        .AsNoTracking()
        .Where(c =>
            c.ResolutionDeadline.HasValue &&
            c.ResolutionDeadline.Value <= currentTime &&
            c.Status != "Resolved" &&
            !c.EscalatedAt.HasValue)
        .OrderBy(c => c.ResolutionDeadline)
        .ToListAsync();

    return View(overdueComplaints);
}
// GET: /Admin/EscalatedComplaints
public async Task<IActionResult> EscalatedComplaints()
{
    var escalatedComplaints = await _context.Complaints
        .AsNoTracking()
        .Where(c =>
            c.EscalatedAt.HasValue &&
            c.Status != "Resolved")
        .OrderByDescending(c => c.EscalatedAt)
        .ToListAsync();

    return View(escalatedComplaints);
}
        // GET: /Admin/Details/3
public async Task<IActionResult> Details(int id)
{
    var complaint = await _context.Complaints
        .FirstOrDefaultAsync(c => c.Id == id);

    if (complaint == null)
    {
        return NotFound();
    }
// Retrieve resolution photographs for this complaint.
var resolutionEvidence = await _context.ComplaintResolutionEvidences
    .AsNoTracking()
    .Where(e => e.ComplaintId == id)
    .OrderByDescending(e => e.UploadedAt)
    .ToListAsync();

ViewBag.ResolutionEvidence = resolutionEvidence;
// Retrieve objections awaiting administrator review.
var pendingResponses = await _context.ComplaintResolutionResponses
    .AsNoTracking()
    .Where(r =>
        r.ComplaintId == id &&
        r.Decision == "NotResolved" &&
        r.ReviewStatus == "Pending")
    .OrderByDescending(r => r.RespondedAt)
    .ToListAsync();

ViewBag.PendingResolutionResponses = pendingResponses;
    return View(complaint);
}
// ============================================
// UPLOAD COMPLAINT RESOLUTION PROOF
// ============================================
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UploadResolutionProof(
    int id,
    IFormFile? file,
    IFormFile? cameraFile)
{
    if (id <= 0)
    {
        return NotFound();
    }

    var complaint = await _context.Complaints
        .FirstOrDefaultAsync(c => c.Id == id);

    if (complaint == null)
    {
        return NotFound();
    }
// Resolution proof can only be uploaded for resolved complaints.
if (!string.Equals(
        complaint.Status,
        "Resolved",
        StringComparison.OrdinalIgnoreCase))
{
    TempData["ResolutionProofError"] =
        "Please mark this complaint as Resolved before uploading resolution proof.";

    return RedirectToAction(nameof(Details), new { id });
}
// Accept an image from either gallery or camera.
file ??= cameraFile;
    if (file == null || file.Length == 0)
    {
        TempData["ResolutionProofError"] =
            "Please select a resolution photograph.";
            

        return RedirectToAction(nameof(Details), new { id });
    }

    // Read existing upload settings.
    var settings = await _context.ComplaintUploadSettings
        .AsNoTracking()
        .FirstOrDefaultAsync() ?? new ComplaintUploadSettings();

    // Validate image format.
    var extension = Path.GetExtension(file.FileName)
        .ToLowerInvariant();

    var allowedExtensions = new[]
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    if (!allowedExtensions.Contains(extension))
    {
        TempData["ResolutionProofError"] =
            "Only JPG, JPEG, PNG and WEBP images are allowed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Check file size using the administrator's upload setting.
    long maximumSize =
        Math.Max(1, settings.MaximumImageSizeMb) * 1024L * 1024L;

    if (file.Length > maximumSize)
    {
        TempData["ResolutionProofError"] =
            $"Image size cannot exceed {settings.MaximumImageSizeMb} MB.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // Create a secure upload directory.
    var webRootPath = _environment.WebRootPath;

    if (string.IsNullOrWhiteSpace(webRootPath))
    {
        webRootPath = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot");
    }

    var uploadFolder = Path.Combine(
        webRootPath,
        "uploads",
        "resolution-evidence");

    Directory.CreateDirectory(uploadFolder);

    // Generate a unique filename.
    var fileName = Guid.NewGuid().ToString("N") + extension;

    var filePath = Path.Combine(uploadFolder, fileName);

    using (var stream = new FileStream(
        filePath,
        FileMode.CreateNew))
    {
        await file.CopyToAsync(stream);
    }

    // Save evidence information in the database.
    var resolutionEvidence = new ComplaintResolutionEvidence
    {
        ComplaintId = complaint.Id,
        ImagePath = "/uploads/resolution-evidence/" + fileName,
        UploadedAt = DateTime.Now,
        UploadedByUserId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
    };

    _context.ComplaintResolutionEvidences.Add(resolutionEvidence);

    await _context.SaveChangesAsync();

    TempData["ResolutionProofSuccess"] =
        "Resolution photograph uploaded successfully.";

    return RedirectToAction(nameof(Details), new { id });
}
// ============================================
// ADMIN REVIEW OF RESOLUTION OBJECTION
// ============================================
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReviewResolutionResponse(
    int responseId,
    string decision,
    string? adminRemarks)
{
    var response = await _context.ComplaintResolutionResponses
        .Include(r => r.Complaint)
        .FirstOrDefaultAsync(r => r.Id == responseId);

    if (response == null || response.Complaint == null)
    {
        return NotFound();
    }

    var complaint = response.Complaint;

    // Only pending objections can be reviewed.
    if (response.Decision != "NotResolved" ||
        response.ReviewStatus != "Pending")
    {
        TempData["ReviewMessage"] =
            "This resolution objection has already been reviewed.";

        return RedirectToAction(nameof(Details),
            new { id = complaint.Id });
    }

    if (decision != "Reopened" && decision != "Rejected")
    {
        return BadRequest("Invalid review decision.");
    }

    if (!string.IsNullOrWhiteSpace(adminRemarks) &&
        adminRemarks.Length > 1000)
    {
        TempData["ReviewMessage"] =
            "Administrator remarks cannot exceed 1000 characters.";

        return RedirectToAction(nameof(Details),
            new { id = complaint.Id });
    }

    response.ReviewStatus = decision;
    response.AdminRemarks = adminRemarks?.Trim();
    response.ReviewedByUserId =
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    response.ReviewedAt = DateTime.Now;

    string historyRemarks;

    if (decision == "Reopened")
    {
        // Restore the complaint to the authority's work queue.
        complaint.Status = "In Progress";
        complaint.ResolvedAt = null;

        historyRemarks =
            "Administrator approved the citizen's objection. Complaint reopened for further action.";

        if (!string.IsNullOrWhiteSpace(adminRemarks))
        {
            historyRemarks += " Admin remarks: " + adminRemarks.Trim();
        }

        TempData["ReviewMessage"] =
            "Resolution objection approved. Complaint reopened successfully.";
    }
    else
    {
        // Keep the complaint resolved.
        historyRemarks =
            "Administrator rejected the citizen's resolution objection.";

        if (!string.IsNullOrWhiteSpace(adminRemarks))
        {
            historyRemarks += " Admin remarks: " + adminRemarks.Trim();
        }

        TempData["ReviewMessage"] =
            "Resolution objection rejected. Complaint remains resolved.";
    }

    var history = new ComplaintStatusHistory
    {
        ComplaintId = complaint.Id,
        Status = complaint.Status,
        Department = complaint.Department,
        Priority = complaint.Priority,
        ChangedAt = DateTime.Now,
        Remarks = historyRemarks
    };

    _context.ComplaintStatusHistories.Add(history);

    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Details),
        new { id = complaint.Id });
}
// POST: /Admin/UpdateStatus
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateStatus(
    int id,
    string status,
    string? department,
    string priority)
{
    var complaint = await _context.Complaints
        .FirstOrDefaultAsync(c => c.Id == id);

    if (complaint == null)
    {
        return NotFound();
    }

    // Check whether anything actually changed
    bool statusChanged = complaint.Status != status;
    bool departmentChanged = complaint.Department != department;
    bool priorityChanged = complaint.Priority != priority;

    // Update complaint
    complaint.Status = status;
    complaint.Department = department;
    complaint.Priority = priority;

    // Set resolved date
    if (status == "Resolved")
    {
        if (!complaint.ResolvedAt.HasValue)
        {
            complaint.ResolvedAt = DateTime.Now;
        }
    }
    else
    {
        complaint.ResolvedAt = null;
    }

    // Only create history when something actually changed
    if (statusChanged || departmentChanged || priorityChanged)
    {
        var history = new ComplaintStatusHistory
        {
            ComplaintId = complaint.Id,
            Status = status,
            Department = department,
            Priority = priority,
            ChangedAt = DateTime.Now,
            Remarks = "Complaint updated by authority"
        };

        _context.ComplaintStatusHistories.Add(history);
    }

    await _context.SaveChangesAsync();

    return RedirectToAction(
        "Details",
        new { id = complaint.Id }
    );
}
    }
}
