
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PublicGrievanceSystem.ViewModels;

namespace PublicGrievanceSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CitizenManagementController : Controller
    {

        private const string SuspensionTokenProvider = "CivicTraceAdmin";
        private const string SuspensionReasonToken = "SuspensionReason";
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CitizenManagementController> _logger;

        public CitizenManagementController(
            UserManager<IdentityUser> userManager,
            ILogger<CitizenManagementController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var users = await _userManager.GetUsersInRoleAsync("Citizen");

            var model = new CitizenManagementViewModel
            {
                Search = search
            };

            foreach (var user in users)
            {
                var claims = await _userManager.GetClaimsAsync(user);

                var name = claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                    ?? user.UserName
                    ?? "Unknown";

                var suspended = user.LockoutEnd.HasValue &&
                    user.LockoutEnd.Value >
                    DateTimeOffset.UtcNow.AddYears(1);
                    var suspensionReason =
    await _userManager.GetAuthenticationTokenAsync(
        user,
        SuspensionTokenProvider,
        SuspensionReasonToken);

                model.Citizens.Add(new CitizenAccountViewModel
{
    Id = user.Id,
    Name = name,
    Email = user.Email ?? "",
    PhoneNumber = user.PhoneNumber ?? "",
    EmailConfirmed = user.EmailConfirmed,
    IsSuspended = suspended,
    SuspensionReason = suspensionReason ?? string.Empty
});
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var query = search.Trim();

                model.Citizens = model.Citizens
                    .Where(c =>
                        c.Name.Contains(
                            query, StringComparison.OrdinalIgnoreCase) ||
                        c.Email.Contains(
                            query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            model.Citizens = model.Citizens
                .OrderBy(c => c.Name)
                .ToList();

            return View(model);
        }

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Suspend(string id, string? reason)
{
    reason = reason?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(id))
        return BadRequest();

    if (string.IsNullOrWhiteSpace(reason))
    {
        TempData["CitizenManagementError"] =
            "A reason is required to suspend a citizen account.";

        return RedirectToAction(nameof(Index));
    }

    if (reason.Length > 500)
    {
        TempData["CitizenManagementError"] =
            "The suspension reason cannot exceed 500 characters.";

        return RedirectToAction(nameof(Index));
    }

    var user = await _userManager.FindByIdAsync(id);

    if (user == null ||
        !await _userManager.IsInRoleAsync(user, "Citizen"))
    {
        return NotFound();
    }

    var enableResult =
        await _userManager.SetLockoutEnabledAsync(user, true);

    if (!enableResult.Succeeded)
    {
        TempData["CitizenManagementError"] =
            "Could not enable account suspension.";

        return RedirectToAction(nameof(Index));
    }

    var lockoutResult =
        await _userManager.SetLockoutEndDateAsync(
            user, DateTimeOffset.MaxValue);

    if (!lockoutResult.Succeeded)
    {
        TempData["CitizenManagementError"] =
            "The citizen account could not be suspended.";

        return RedirectToAction(nameof(Index));
    }

    var reasonResult =
        await _userManager.SetAuthenticationTokenAsync(
            user,
            SuspensionTokenProvider,
            SuspensionReasonToken,
            reason);

    if (!reasonResult.Succeeded)
    {
        // Roll back the suspension if the reason cannot be saved.
        await _userManager.SetLockoutEndDateAsync(user, null);

        TempData["CitizenManagementError"] =
            "The suspension reason could not be saved. The account was not suspended.";

        return RedirectToAction(nameof(Index));
    }

    _logger.LogInformation(
        "Administrator suspended citizen account {UserId}.",
        user.Id);

    TempData["CitizenManagementMessage"] =
        "Citizen account suspended successfully.";

    return RedirectToAction(nameof(Index));
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null ||
                !await _userManager.IsInRoleAsync(user, "Citizen"))
            {
                return NotFound();
            }

            var result = await _userManager.SetLockoutEndDateAsync(
                user, null);

            if (!result.Succeeded)
            {
                TempData["CitizenManagementError"] =
                    "The citizen account could not be reactivated.";

                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation(
                "Administrator reactivated citizen account {UserId}.",
                user.Id);

            TempData["CitizenManagementMessage"] =
                "Citizen account reactivated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
