
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PublicGrievanceSystem.Areas.Identity.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public ResetPasswordModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? LoginType { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool InvalidLink { get; private set; }

    public class InputModel
    {
        [Required]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "The password must contain at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("Password",
            ErrorMessage = "The password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Code) ||
            (LoginType != "Admin" && LoginType != "Citizen"))
        {
            InvalidLink = true;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Code) ||
            (LoginType != "Admin" && LoginType != "Citizen"))
        {
            InvalidLink = true;
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Email);

        if (user == null ||
            !await _userManager.IsInRoleAsync(user, LoginType))
        {
            InvalidLink = true;
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(
            user,
            Code,
            Input.Password);

        if (result.Succeeded)
        {
            return RedirectToPage(
                "./ResetPasswordConfirmation",
                new { loginType = LoginType });
        }

        InvalidLink = true;

        ModelState.AddModelError(
            string.Empty,
            "The reset link may be invalid or expired, or the new password may not meet the password requirements.");

        return Page();
    }
}
