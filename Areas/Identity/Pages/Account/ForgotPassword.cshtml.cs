
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PublicGrievanceSystem.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordModel(
        UserManager<IdentityUser> userManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LoginType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
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

        if (user != null &&
            await _userManager.IsInRoleAsync(user, LoginType))
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    code,
                    email = Input.Email,
                    loginType = LoginType
                },
                protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                throw new InvalidOperationException(
                    "Unable to generate the password-reset URL.");
            }

            var safeUrl = HtmlEncoder.Default.Encode(callbackUrl);

            var message = $"""
                <p>Hello,</p>
                <p>We received a request to reset your CivicTrace password.</p>
                <p>
                    <a href="{safeUrl}"
                       style="display:inline-block;padding:12px 20px;
                       background-color:#0d6efd;color:#ffffff;
                       text-decoration:none;border-radius:6px;">
                       Reset Password
                    </a>
                </p>
                <p>If you did not request this, you can ignore this email.</p>
                <p>Regards,<br>CivicTrace Team</p>
                """;

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Reset your CivicTrace password",
                message);
        }

        // Show the same confirmation even if the account or role
        // does not match, avoiding disclosure of account details.
        return RedirectToPage(
            "./ForgotPasswordConfirmation",
            new
            {
                loginType = LoginType,
                returnUrl = ReturnUrl
            });
    }
}
