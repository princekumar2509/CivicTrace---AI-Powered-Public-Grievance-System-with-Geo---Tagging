// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;

namespace PublicGrievanceSystem.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private const string OtpTokenProvider = "CivicTraceEmailOtp";
        private const string OtpRequiredToken = "OtpVerificationRequired";
private const string OtpHashToken = "OtpHash";
private const string OtpExpiryToken = "OtpExpiryUtc";
private const string OtpAttemptsToken = "OtpAttempts";
private const string OtpSentAtToken = "OtpSentAtUtc";
private const string OtpWindowStartToken = "OtpWindowStartUtc";
private const string OtpSendCountToken = "OtpSendCount";
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = "";
            [Required(ErrorMessage = "Email address is required.")]
[EmailAddress(ErrorMessage = "Please enter a valid email address.")]

[Display(Name = "Email")]
public string Email { get; set; } = "";

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
{
    returnUrl ??= Url.Content("~/");

    ExternalLogins =
        (await _signInManager.GetExternalAuthenticationSchemesAsync())
        .ToList();

    if (!ModelState.IsValid)
    {
        return Page();
    }

    Input.Email = Input.Email.Trim();

    var user = CreateUser();

    await _userStore.SetUserNameAsync(
        user, Input.Email, CancellationToken.None);

    await _emailStore.SetEmailAsync(
        user, Input.Email, CancellationToken.None);

    user.EmailConfirmed = false;

    var result = await _userManager.CreateAsync(
        user, Input.Password);

    if (!result.Succeeded)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty, error.Description);
        }

        return Page();
    }

    var nameClaimResult = await _userManager.AddClaimAsync(
        user,
        new Claim(ClaimTypes.Name, Input.Name.Trim()));

    if (!nameClaimResult.Succeeded)
    {
        _logger.LogWarning(
            "Account created, but saving the name claim failed.");
    }

    var roleResult = await _userManager.AddToRoleAsync(
        user, "Citizen");

    if (!roleResult.Succeeded)
    {
        _logger.LogWarning(
            "Account created, but assigning the Citizen role failed.");
    }

    _logger.LogInformation(
        "A new unconfirmed CivicTrace account was created.");

    try
    {
        await SendEmailOtpAsync(user);

        TempData["OtpNotice"] =
            "A six-digit verification code has been sent to your email.";
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex, "Failed to send the registration verification code.");

        TempData["OtpError"] =
            "We could not send the code. Open the verification page and use Resend Code to try again.";
    }

    return RedirectToPage(
        "./VerifyEmailOtp",
        new
        {
            email = Input.Email,
            returnUrl = returnUrl
        });
}
private async Task SendEmailOtpAsync(IdentityUser user)
{
    await SaveOtpTokenAsync(
        user,
        OtpRequiredToken,
        "true");
    var now = DateTime.UtcNow;

    var otp = RandomNumberGenerator
        .GetInt32(0, 1_000_000)
        .ToString("D6", CultureInfo.InvariantCulture);

    var otpHash = _userManager.PasswordHasher
        .HashPassword(user, otp);

    await SaveOtpTokenAsync(
        user, OtpHashToken, otpHash);

    await SaveOtpTokenAsync(
        user,
        OtpExpiryToken,
        now.AddMinutes(5).ToString(
            "O", CultureInfo.InvariantCulture));

    await SaveOtpTokenAsync(
        user, OtpAttemptsToken, "0");

    await SaveOtpTokenAsync(
        user,
        OtpSentAtToken,
        now.ToString("O", CultureInfo.InvariantCulture));

    await SaveOtpTokenAsync(
        user,
        OtpWindowStartToken,
        now.ToString("O", CultureInfo.InvariantCulture));

    await SaveOtpTokenAsync(
        user, OtpSendCountToken, "1");

    var emailBody = $@"
        <div style='font-family:Arial,sans-serif;
                    max-width:560px;margin:auto;padding:24px;
                    color:#23364d;'>
            <h2 style='color:#153b6d;'>CivicTrace Email Verification</h2>

            <p>Welcome to CivicTrace!</p>

            <p>Use the following six-digit code to verify your email address:</p>

            <div style='font-size:32px;font-weight:bold;
                        letter-spacing:8px;text-align:center;
                        padding:18px;background:#eaf2ff;
                        border-radius:10px;color:#153b6d;'>
                {otp}
            </div>

            <p style='margin-top:20px;'>
                This code expires in <strong>5 minutes</strong>.
            </p>

            <p>If you did not request this code, you can ignore this email.</p>

            <p>Regards,<br/>CivicTrace Team</p>
        </div>";

    try
    {
        await _emailSender.SendEmailAsync(
            user.Email,
            "Your CivicTrace verification code",
            emailBody);
    }
    catch
    {
        // Allow the user to request another code immediately
        // if the email service fails.
        await _userManager.RemoveAuthenticationTokenAsync(
            user, OtpTokenProvider, OtpSentAtToken);

        throw;
    }
}

private async Task SaveOtpTokenAsync(
    IdentityUser user,
    string tokenName,
    string value)
{
    var result = await _userManager.SetAuthenticationTokenAsync(
        user,
        OtpTokenProvider,
        tokenName,
        value);

    if (!result.Succeeded)
    {
        throw new InvalidOperationException(
            "Could not save email verification information.");
    }
}
        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
