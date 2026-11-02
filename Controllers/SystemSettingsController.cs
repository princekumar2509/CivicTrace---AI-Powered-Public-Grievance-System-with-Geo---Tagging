
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PublicGrievanceSystem.Data;
using PublicGrievanceSystem.Models;

namespace PublicGrievanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SystemSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.ComplaintUploadSettings
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new ComplaintUploadSettings();

                _context.ComplaintUploadSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            ComplaintUploadSettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var settings = await _context.ComplaintUploadSettings
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new ComplaintUploadSettings();
                _context.ComplaintUploadSettings.Add(settings);
            }

            settings.MaximumImageSizeMb = model.MaximumImageSizeMb;
            settings.AllowedImageFormats = model.AllowedImageFormats
                .Split(',')
                .Select(format => format.Trim().TrimStart('.'))
                .Where(format => !string.IsNullOrWhiteSpace(format))
                .Select(format => format.ToLowerInvariant())
                .Distinct()
                .Aggregate(
                    (current, next) => current + "," + next);

            settings.RequireImage = model.RequireImage;
            settings.AllowMultipleImages = model.AllowMultipleImages;
            settings.MaximumImagesPerComplaint =
                model.MaximumImagesPerComplaint;
                // Save complaint workflow settings
settings.AllowNewComplaints = model.AllowNewComplaints;
settings.AllowCitizensToReopenComplaints =
    model.AllowCitizensToReopenComplaints;
settings.RequireComplaintDescription =
    model.RequireComplaintDescription;
settings.RequireLocation = model.RequireLocation;
settings.AllowAnonymousComplaints =
    model.AllowAnonymousComplaints;
settings.EnableDuplicateDetection =
    model.EnableDuplicateDetection;
settings.EnableCitizenFeedback =
    model.EnableCitizenFeedback;
    // Save Resolution & Escalation Settings

settings.HighPriorityResolutionHours =
    model.HighPriorityResolutionHours;

settings.MediumPriorityResolutionHours =
    model.MediumPriorityResolutionHours;

settings.LowPriorityResolutionHours =
    model.LowPriorityResolutionHours;

settings.EnableAutomaticOverdueDetection =
    model.EnableAutomaticOverdueDetection;

settings.EnableEscalationNotifications =
    model.EnableEscalationNotifications;

settings.EscalationAfterDeadlineHours =
    model.EscalationAfterDeadlineHours;

settings.EnableResolutionProof =
    model.EnableResolutionProof;

            await _context.SaveChangesAsync();

            TempData["SettingsSuccess"] =
                "System settings saved successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
