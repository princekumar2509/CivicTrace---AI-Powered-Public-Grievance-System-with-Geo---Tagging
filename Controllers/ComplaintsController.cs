using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Data;
using PublicGrievanceSystem.Models;
using PublicGrievanceSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PublicGrievanceSystem.Controllers
{
    [Authorize]
    public class ComplaintsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly AIComplaintClassifier _aiClassifier;
        private readonly DuplicateComplaintDetector _duplicateDetector;
        private readonly AIImageAnalyzer _aiImageAnalyzer;
        private readonly AISeverityPredictor _aiSeverityPredictor;
        private readonly PendingComplaintUploadStore _pendingUploadStore;

        public ComplaintsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            AIComplaintClassifier aiClassifier,
            DuplicateComplaintDetector duplicateDetector,
            AIImageAnalyzer aiImageAnalyzer,
AISeverityPredictor aiSeverityPredictor,
PendingComplaintUploadStore pendingUploadStore)
        {
            _context = context;
            _environment = environment;
            _aiClassifier = aiClassifier;
            _duplicateDetector = duplicateDetector;
            _aiImageAnalyzer = aiImageAnalyzer;
            _aiSeverityPredictor = aiSeverityPredictor;
            _pendingUploadStore = pendingUploadStore;
        }

        // =========================
        // CREATE COMPLAINT - GET
        // =========================

        [Authorize(Roles = "Citizen")]
public async Task<IActionResult> Create()
{
    var uploadSettings = await _context.ComplaintUploadSettings
        .AsNoTracking()
        .FirstOrDefaultAsync() ?? new ComplaintUploadSettings();

    if (!uploadSettings.AllowNewComplaints)
    {
        Response.StatusCode = 503;
        return View("SubmissionUnavailable");
    }

    return View();
}


        // =========================
        // CREATE COMPLAINT - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> Create(
            Complaint complaint,
            List<IFormFile>? ImageFiles,
            bool forceSubmit = false,
    string? temporaryUploadToken = null)
        {
            // Category is generated automatically by AI classifier
            ModelState.Remove(nameof(Complaint.Category));
            
var uploadSettings = await _context.ComplaintUploadSettings
    .AsNoTracking()
    .FirstOrDefaultAsync() ?? new ComplaintUploadSettings();
    // Block direct submissions when administrators disable complaints.
if (!uploadSettings.AllowNewComplaints)
{
    Response.StatusCode = 503;
    return View("SubmissionUnavailable");
}
// =====================================================
// VALIDATE COMPLAINT DESCRIPTION AND LOCATION
// =====================================================

// Allow the admin setting to control description validation.
ModelState.Remove(nameof(Complaint.Description));

if (uploadSettings.RequireComplaintDescription &&
    string.IsNullOrWhiteSpace(complaint.Description))
{
    ModelState.AddModelError(
        nameof(Complaint.Description),
        "Please provide a description of the complaint.");
}

// Check the latitude and longitude received from the form.
double? latitude = complaint.Latitude;
double? longitude = complaint.Longitude;

bool hasValidLocation =
    latitude.HasValue &&
    longitude.HasValue &&
    latitude.Value >= -90 &&
    latitude.Value <= 90 &&
    longitude.Value >= -180 &&
    longitude.Value <= 180 &&
    !(latitude.Value == 0 && longitude.Value == 0);

if (uploadSettings.RequireLocation && !hasValidLocation)
{
    ModelState.AddModelError(
        string.Empty,
        "Please select a valid complaint location on the map.");
}
var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

if (string.IsNullOrWhiteSpace(currentUserId))
{
    return Forbid();
}

var uploadedImages = (ImageFiles ?? new List<IFormFile>())
    .Where(file => file != null && file.Length > 0)
    .ToList();

var restoredImageStreams = new List<MemoryStream>();

// Restore previously uploaded images when the citizen clicks Submit Anyway.
if (uploadedImages.Count == 0 &&
    !string.IsNullOrWhiteSpace(temporaryUploadToken))
{
    var pendingImages = await _pendingUploadStore.RestoreAsync(
        temporaryUploadToken,
        currentUserId);

    foreach (var pendingImage in pendingImages)
    {
        var stream = new MemoryStream(pendingImage.Content);
        restoredImageStreams.Add(stream);

        uploadedImages.Add(new FormFile(
            stream,
            0,
            stream.Length,
            "ImageFiles",
            pendingImage.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = pendingImage.ContentType
        });
    }
}

// Keep the first image available for the existing AI analysis code.
var ImageFile = uploadedImages.FirstOrDefault();

bool hasImage = uploadedImages.Count > 0;

if (uploadSettings.RequireImage && !hasImage)
{
    ModelState.AddModelError(
        string.Empty,
        "Please upload at least one evidence image.");
}

if (!uploadSettings.AllowMultipleImages && uploadedImages.Count > 1)
{
    ModelState.AddModelError(
        string.Empty,
        "Multiple images are currently disabled. Please select one image.");
}

int maximumImages = uploadSettings.AllowMultipleImages
    ? Math.Max(1, uploadSettings.MaximumImagesPerComplaint)
    : 1;

if (uploadedImages.Count > maximumImages)
{
    ModelState.AddModelError(
        string.Empty,
        $"You can upload a maximum of {maximumImages} image(s) per complaint.");
}

long maximumBytes =
    (long)Math.Max(1, uploadSettings.MaximumImageSizeMb)
    * 1024L * 1024L;

var allowedFormats = (uploadSettings.AllowedImageFormats ?? "")
    .Split(
        ',',
        StringSplitOptions.RemoveEmptyEntries |
        StringSplitOptions.TrimEntries)
    .Select(format => format.TrimStart('.').ToLowerInvariant())
    .Where(format => !string.IsNullOrWhiteSpace(format))
    .ToArray();

foreach (var file in uploadedImages)
{
    if (file.Length > maximumBytes)
    {
        ModelState.AddModelError(
            string.Empty,
            $"The file '{file.FileName}' exceeds the maximum size of " +
            $"{uploadSettings.MaximumImageSizeMb} MB.");
    }

    string extension = Path.GetExtension(file.FileName)
        .TrimStart('.')
        .ToLowerInvariant();

    if (allowedFormats.Length == 0 ||
        !allowedFormats.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase))
    {
        ModelState.AddModelError(
            string.Empty,
            $"The file '{file.FileName}' has an unsupported format. " +
            $"Allowed formats: {string.Join(", ", allowedFormats)}.");
    }
}


            if (!ModelState.IsValid)
            {
                return View(complaint);
            }


            // =====================================================
            // STEP 1: TEXT AI CLASSIFICATION
            // =====================================================

            var aiResult = _aiClassifier.Classify(
                complaint.Title,
                complaint.Description);

            complaint.Category = aiResult.Category;
            complaint.Department = aiResult.Department;
            complaint.Priority = aiResult.Priority;
            // =====================================================
// STEP 1B: AI SEVERITY PREDICTION
// =====================================================

complaint.AISeverity =
    _aiSeverityPredictor.PredictSeverity(
        complaint.Title,
        complaint.Description,
        complaint.Category);


            // =====================================================
// STEP 2: READ IMAGE ONCE
// =====================================================

byte[]? imageBytes = null;
string? mimeType = null;

if (ImageFile != null && ImageFile.Length > 0)
{
    using var memoryStream = new MemoryStream();

    await ImageFile.CopyToAsync(memoryStream);

    imageBytes = memoryStream.ToArray();

    mimeType = ImageFile.ContentType;

    /* ViewBag.ImageReceived =
        $"Image received: {ImageFile.FileName} ({ImageFile.Length} bytes)"; */
}
/* else
{
    ViewBag.ImageReceived =
        "No image was received by the server.";
} */


            
            // =====================================================
            // STEP 3: CHECK DUPLICATE COMPLAINTS
            // =====================================================

            if (uploadSettings.EnableDuplicateDetection)
            {
                var existingComplaints =
                    await _context.Complaints
                        .Where(c => c.Id != complaint.Id)
                        .ToListAsync();

                var duplicateMatches =
                    _duplicateDetector.FindDuplicates(
                        complaint,
                        existingComplaints);

                // Show the warning unless the citizen chooses
                // to submit the complaint anyway.
                if (duplicateMatches.Count > 0 && !forceSubmit)
{
    if (uploadedImages.Count > 0)
    {
        ViewBag.TemporaryUploadToken =
            await _pendingUploadStore.SaveAsync(
                uploadedImages,
                currentUserId);
    }

    ViewBag.DuplicateMatches = duplicateMatches;
    return View("Create", complaint);
}
            }



            // =====================================================
// STEP 5: SET COMPLAINT STATUS & USER OWNERSHIP
// =====================================================

complaint.Status = "Submitted";
complaint.CreatedAt = DateTime.Now;
// =====================================================
// CALCULATE RESOLUTION DEADLINE
// =====================================================

int resolutionHours = complaint.Priority switch
{
    "High" => uploadSettings.HighPriorityResolutionHours,
    "Medium" => uploadSettings.MediumPriorityResolutionHours,
    "Low" => uploadSettings.LowPriorityResolutionHours,
    _ => uploadSettings.MediumPriorityResolutionHours
};

complaint.ResolutionDeadline =
    complaint.CreatedAt.AddHours(resolutionHours);

// Reset escalation and resolution tracking for new complaint.
complaint.EscalatedAt = null;
complaint.ResolvedAt = null;

// Store the logged-in citizen's Identity information
complaint.UserId =
    User.FindFirstValue(ClaimTypes.NameIdentifier);

complaint.UserEmail =
    User.FindFirstValue(ClaimTypes.Email);


            
            // =====================================================
            // STEP 6: SAVE ALL SELECTED EVIDENCE IMAGES
            // =====================================================

            var savedImagePaths = new List<string>();

            if (uploadedImages.Count > 0)
            {
                string uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads");

                Directory.CreateDirectory(uploadsFolder);

                foreach (var uploadedImage in uploadedImages)
                {
                    string extension = Path.GetExtension(
                        uploadedImage.FileName).ToLowerInvariant();

                    string fileName = Guid.NewGuid().ToString()
                        + extension;

                    string filePath = Path.Combine(
                        uploadsFolder,
                        fileName);

                    await using (var fileStream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await uploadedImage.CopyToAsync(fileStream);
                    }

                    savedImagePaths.Add("/uploads/" + fileName);
                }

                // Preserve compatibility with existing features.
                complaint.ImagePath = savedImagePaths.FirstOrDefault();
            }



            // =====================================================
            // STEP 7: SAVE COMPLAINT FIRST
            // =====================================================

            _context.Complaints.Add(complaint);

            await _context.SaveChangesAsync();

// =====================================================
// SAVE EVIDENCE RECORDS FOR ALL UPLOADED IMAGES
// =====================================================

if (savedImagePaths.Count > 0)
{
    var evidenceRecords = savedImagePaths.Select(path =>
        new ComplaintEvidence
        {
            ComplaintId = complaint.Id,
            ImagePath = path,
            UploadedAt = DateTime.Now
        });

    _context.ComplaintEvidences.AddRange(evidenceRecords);

    await _context.SaveChangesAsync();
}
            // =====================================================
            // STEP 8: SAVE INITIAL HISTORY
            // =====================================================

            var history =
                new ComplaintStatusHistory
                {
                    ComplaintId = complaint.Id,
                    Status = complaint.Status,
                    Department = complaint.Department,
                    Priority = complaint.Priority,
                    ChangedAt = complaint.CreatedAt,
                    Remarks = "Complaint submitted"
                };

            _context.ComplaintStatusHistories.Add(history);

            await _context.SaveChangesAsync();


            // =====================================================
            // STEP 9: GEMINI AI IMAGE ANALYSIS
            // =====================================================

            if (imageBytes != null &&
                imageBytes.Length > 0 &&
                !string.IsNullOrWhiteSpace(mimeType))
            {
                try
                {
                    // Give Gemini a maximum of 10 seconds
                    using var aiTimeout =
                        new CancellationTokenSource(
                            TimeSpan.FromSeconds(40));

                    var imageAnalysis =
                        await _aiImageAnalyzer.AnalyzeImageAsync(
                            imageBytes,
                            mimeType,
                            aiTimeout.Token);


                    // =============================================
                    // SAVE GEMINI RESULT
                    // =============================================

                    complaint.AIImageCategory =
                        imageAnalysis.Category;

                    complaint.AIImageConfidence =
                        imageAnalysis.Confidence;

                    complaint.AIImageDescription =
                        imageAnalysis.Description;


                    await _context.SaveChangesAsync();


                    // =============================================
                    // SEND RESULT TO SUCCESS PAGE
                    // =============================================

                    ViewBag.ImageCategory =
                        imageAnalysis.Category;

                    ViewBag.ImageConfidence =
                        imageAnalysis.Confidence;

                    ViewBag.ImageDescription =
                        imageAnalysis.Description;
                }
                catch (OperationCanceledException)
                {
                    ViewBag.ImageAnalysisError =
                        "AI image analysis timed out. " +
                        "The complaint was still submitted successfully.";
                }
                catch (Exception ex)
{
    ViewBag.ImageAnalysisError =
        "AI image analysis failed: " +
        ex.Message +
        " The complaint was still submitted successfully.";
}
            }


            // =====================================================
            // STEP 10: SHOW SUCCESS PAGE
            // =====================================================
// Close streams used to restore the temporary images.
foreach (var stream in restoredImageStreams)
{
    await stream.DisposeAsync();
}

// Delete the temporary upload after successful submission.
if (!string.IsNullOrWhiteSpace(temporaryUploadToken))
{
    await _pendingUploadStore.DeleteAsync(
        temporaryUploadToken,
        currentUserId);
}
            return View("Success", complaint);
        }


        // =========================
        // TRACK COMPLAINT - GET
        // =========================
        [Authorize(Roles = "Citizen")]
public async Task<IActionResult> Track(int? id)
{
    // If no complaint ID was provided,
    // show the normal tracking form.
    if (!id.HasValue)
    {
        return View();
    }

    if (id.Value <= 0)
    {
        ViewBag.Error = "Please enter a valid complaint ID.";
        return View();
    }

    var currentUserId =
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    var complaint =
    await _context.Complaints
        .Include(c => c.Evidences)
        .FirstOrDefaultAsync(
            c => c.Id == id.Value &&
                c.UserId == currentUserId);

    if (complaint == null)
    {
        ViewBag.Error =
            $"Complaint #{id.Value} was not found.";

        return View();
    }

    ViewBag.StatusHistory =
        await _context.ComplaintStatusHistories
            .Where(h => h.ComplaintId == complaint.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

    return View(complaint);
}


        // =========================
        // TRACK COMPLAINT - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> Track(int id)
        {
            if (id <= 0)
            {
                ViewBag.Error =
                    "Please enter a valid complaint ID.";

                return View();
            }


            var currentUserId =
    User.FindFirstValue(ClaimTypes.NameIdentifier);

var complaint =
    await _context.Complaints
        .Include(c => c.Evidences)
        .FirstOrDefaultAsync(
            c => c.Id == id &&
                c.UserId == currentUserId);


            if (complaint == null)
            {
                ViewBag.Error =
                    $"Complaint #{id} was not found.";

                return View();
            }


            ViewBag.StatusHistory =
                await _context.ComplaintStatusHistories
                    .Where(
                        h => h.ComplaintId ==
                             complaint.Id)
                    .OrderBy(
                        h => h.ChangedAt)
                    .ToListAsync();


            return View(complaint);
        }
    }
}
