
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PublicGrievanceSystem.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private const string OtpTokenProvider = "CivicTraceEmailOtp";
private const string OtpRequiredToken = "OtpVerificationRequired";
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public LoginModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LoginType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string PageTitle =>
        LoginType == "Admin" ? "Admin Login" : "Citizen Login";

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public IActionResult OnGet()
    {
        if (LoginType != "Admin" && LoginType != "Citizen")
        {
            return RedirectToPage("/Account/LoginChoice");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (LoginType != "Admin" && LoginType != "Citizen")
        {
            return RedirectToPage("/Account/LoginChoice");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);

        if (user == null ||
            !await _userManager.IsInRoleAsync(user, LoginType))
        {
            ModelState.AddModelError(
                string.Empty,
                LoginType == "Admin"
                    ? "This account does not have administrator access."
                    : "This account is not registered as a citizen.");

            return Page();
        }
// Require email verification for Citizen password logins.
var otpRequired = await _userManager.GetAuthenticationTokenAsync(
    user,
    OtpTokenProvider,
    OtpRequiredToken);

if (LoginType == "Citizen" &&
    otpRequired == "true" &&
    !user.EmailConfirmed)
{
    TempData["OtpNotice"] =
        "Please verify your email address before signing in.";

    return RedirectToPage(
        "./VerifyEmailOtp",
        new
        {
            email = user.Email,
            returnUrl = ReturnUrl
        });
}
        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Redirect administrators directly to Admin Dashboard.
if (LoginType == "Admin")
{
    return RedirectToAction("Index", "Admin");
}

// Preserve the requested local destination for citizens.
if (!string.IsNullOrWhiteSpace(ReturnUrl) &&
    Url.IsLocalUrl(ReturnUrl))
{
    return LocalRedirect(ReturnUrl);
}

// Default citizen destination.
return RedirectToAction("Index", "Citizen");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "This account is temporarily locked. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");
        }

        return Page();
    }
}
