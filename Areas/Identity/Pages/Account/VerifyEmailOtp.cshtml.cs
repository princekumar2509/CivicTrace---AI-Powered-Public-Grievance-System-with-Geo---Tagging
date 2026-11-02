using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace PublicGrievanceSystem.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class VerifyEmailOtpModel : PageModel
    {
        private const string OtpTokenProvider = "CivicTraceEmailOtp";
        private const string OtpRequiredToken = "OtpVerificationRequired";
        private const string OtpHashToken = "OtpHash";
        private const string OtpExpiryToken = "OtpExpiryUtc";
        private const string OtpAttemptsToken = "OtpAttempts";
        private const string OtpSentAtToken = "OtpSentAtUtc";
        private const string OtpWindowStartToken = "OtpWindowStartUtc";
        private const string OtpSendCountToken = "OtpSendCount";

        private const int MaxVerifyAttempts = 5;
        private const int MaxSendsPerHour = 5;

        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<VerifyEmailOtpModel> _logger;

        public VerifyEmailOtpModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<VerifyEmailOtpModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Otp { get; set; } = "";

        [BindProperty]
        public string ReturnUrl { get; set; } = "~/";

        [TempData]
public string StatusMessage { get; set; } = "";

[TempData]
public string ErrorMessage { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(
    string? email,
    string? returnUrl = null)
        {
            Email = email?.Trim() ?? "";
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (string.IsNullOrWhiteSpace(Email))
            {
                return RedirectToPage("./Register");
            }

            var user = await _userManager.FindByEmailAsync(Email);

            if (user == null)
            {
                TempData["OtpError"] =
                    "Registration was not found. Please register again.";

                return RedirectToPage("./Register");
            }

            if (user.EmailConfirmed)
            {
                TempData["OtpSuccess"] =
                    "Your email is already verified. Please sign in.";

                return RedirectToPage(
                    "./Login",
                    new
                    {
                        loginType = "Citizen",
                        returnUrl = ReturnUrl
                    });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVerifyAsync()
        {
            Email = Email?.Trim() ?? "";
            ReturnUrl = ReturnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrEmpty(Otp) ||
                Otp.Length != 6 ||
                !Otp.All(char.IsDigit))
            {
                ModelState.AddModelError(
                    nameof(Otp),
                    "Enter the six-digit verification code.");

                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "We could not verify this registration. Please register again.");

                return Page();
            }

            if (user.EmailConfirmed)
            {
                TempData["OtpSuccess"] = "Your email is already verified.";

                return RedirectToPage(
                    "./Login",
                    new
                    {
                        loginType = "Citizen",
                        returnUrl = ReturnUrl
                    });
            }

            var storedHash = await GetOtpTokenAsync(user, OtpHashToken);
            var expiryText = await GetOtpTokenAsync(user, OtpExpiryToken);
            var attemptsText = await GetOtpTokenAsync(user, OtpAttemptsToken);

            if (string.IsNullOrWhiteSpace(storedHash) ||
                !TryParseUtc(expiryText, out var expiryUtc))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your verification code is unavailable. Please request a new code.");

                return Page();
            }

            if (DateTime.UtcNow >= expiryUtc)
            {
                await ClearVerificationCodeAsync(user);

                ModelState.AddModelError(
                    string.Empty,
                    "Your verification code has expired. Please request a new one.");

                return Page();
            }

            int.TryParse(attemptsText, out var attempts);

            if (attempts >= MaxVerifyAttempts)
            {
                await ClearVerificationCodeAsync(user);

                ModelState.AddModelError(
                    string.Empty,
                    "You have reached the maximum number of attempts. Please request a new code.");

                return Page();
            }

            var nextAttempt = attempts + 1;

            await SaveOtpTokenAsync(
                user,
                OtpAttemptsToken,
                nextAttempt.ToString(CultureInfo.InvariantCulture));

            var verificationResult =
                _userManager.PasswordHasher.VerifyHashedPassword(
                    user,
                    storedHash,
                    Otp);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                if (nextAttempt >= MaxVerifyAttempts)
                {
                    await ClearVerificationCodeAsync(user);

                    ModelState.AddModelError(
                        string.Empty,
                        "The code is incorrect. You have reached the attempt limit; request a new code.");
                }
                else
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"The code is incorrect. {MaxVerifyAttempts - nextAttempt} attempt(s) remaining.");
                }

                return Page();
            }

           user.EmailConfirmed = true;

var updateResult = await _userManager.UpdateAsync(user);

if (!updateResult.Succeeded)
{
    _logger.LogError(
        "Failed to confirm email for user {UserId}.",
        user.Id);

    ModelState.AddModelError(
        string.Empty,
        "We couldn't verify your email right now. Please try again.");

    return Page();
}

            await ClearVerificationCodeAsync(user);
            await _userManager.RemoveAuthenticationTokenAsync(
    user,
    OtpTokenProvider,
    OtpRequiredToken);

            _logger.LogInformation(
                "Email verification completed for account {UserId}.",
                user.Id);

            TempData["OtpSuccess"] =
                "Email verified successfully. You can now sign in.";

            return RedirectToPage(
                "./Login",
                new
                {
                    loginType = "Citizen",
                    returnUrl = ReturnUrl
                });
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            Email = Email?.Trim() ?? "";
            ReturnUrl = ReturnUrl ?? Url.Content("~/");
ModelState.Remove(nameof(Otp));
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "We could not find this registration. Please register again.");

                return Page();
            }

            if (user.EmailConfirmed)
            {
                return RedirectToPage(
                    "./Login",
                    new
                    {
                        loginType = "Citizen",
                        returnUrl = ReturnUrl
                    });
            }

            var now = DateTime.UtcNow;

            var sentAtText =
                await GetOtpTokenAsync(user, OtpSentAtToken);

            if (TryParseUtc(sentAtText, out var sentAt) &&
                now - sentAt < TimeSpan.FromSeconds(60))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please wait 60 seconds between code requests.");

                return Page();
            }

            var windowStartText =
                await GetOtpTokenAsync(user, OtpWindowStartToken);

            var countText =
                await GetOtpTokenAsync(user, OtpSendCountToken);

            DateTime windowStart;
            int sendCount;

            if (!TryParseUtc(windowStartText, out windowStart) ||
                now - windowStart >= TimeSpan.FromHours(1) ||
                now < windowStart)
            {
                windowStart = now;
                sendCount = 0;
            }
            else
            {
                int.TryParse(countText, out sendCount);
            }

            if (sendCount >= MaxSendsPerHour)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You have reached the maximum of five code requests per hour. Please try again later.");

                return Page();
            }

            try
            {
                await SendNewOtpAsync(
                    user,
                    now,
                    windowStart,
                    sendCount + 1);

                StatusMessage =
                    "A new verification code has been sent to your email.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resend registration OTP.");

                ErrorMessage =
                    "We could not send the code. Please try again later.";
            }

            return Page();
        }

        private async Task SendNewOtpAsync(
            IdentityUser user,
            DateTime now,
            DateTime windowStart,
            int sendCount)
        {
            var otp = RandomNumberGenerator
                .GetInt32(0, 1_000_000)
                .ToString("D6", CultureInfo.InvariantCulture);

            var otpHash = _userManager.PasswordHasher
                .HashPassword(user, otp);

            await SaveOtpTokenAsync(user, OtpHashToken, otpHash);

            await SaveOtpTokenAsync(
                user,
                OtpExpiryToken,
                now.AddMinutes(5).ToString(
                    "O", CultureInfo.InvariantCulture));

            await SaveOtpTokenAsync(user, OtpAttemptsToken, "0");

            await SaveOtpTokenAsync(
                user,
                OtpSentAtToken,
                now.ToString("O", CultureInfo.InvariantCulture));

            await SaveOtpTokenAsync(
                user,
                OtpWindowStartToken,
                windowStart.ToString("O", CultureInfo.InvariantCulture));

            await SaveOtpTokenAsync(
                user,
                OtpSendCountToken,
                sendCount.ToString(CultureInfo.InvariantCulture));

            var body = $@"
                <div style='font-family:Arial,sans-serif;
                            max-width:560px;margin:auto;padding:24px;
                            color:#23364d;'>
                    <h2 style='color:#153b6d;'>CivicTrace Email Verification</h2>
                    <p>Your new six-digit verification code is:</p>
                    <div style='font-size:32px;font-weight:bold;
                                letter-spacing:8px;text-align:center;
                                padding:18px;background:#eaf2ff;
                                border-radius:10px;color:#153b6d;'>
                        {otp}
                    </div>
                    <p style='margin-top:20px;'>This code expires in <strong>5 minutes</strong>.</p>
                    <p>If you did not request this code, you can ignore this email.</p>
                    <p>Regards,<br/>CivicTrace Team</p>
                </div>";

            try
            {
                var emailAddress = user.Email;

if (string.IsNullOrWhiteSpace(emailAddress))
{
    throw new InvalidOperationException(
        "This account does not have an email address.");
}

try
{
    await _emailSender.SendEmailAsync(
        emailAddress,
        "Your CivicTrace verification code",
        body);
}
catch
{
    await _userManager.RemoveAuthenticationTokenAsync(
        user,
        OtpTokenProvider,
        OtpSentAtToken);

    throw;
}
            }
            catch
            {
                await _userManager.RemoveAuthenticationTokenAsync(
                    user,
                    OtpTokenProvider,
                    OtpSentAtToken);

                throw;
            }
        }

        private Task<string?> GetOtpTokenAsync(
    IdentityUser user,
    string tokenName)
{
    return _userManager.GetAuthenticationTokenAsync(
        user,
        OtpTokenProvider,
        tokenName);
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

        private async Task ClearVerificationCodeAsync(IdentityUser user)
        {
            await _userManager.RemoveAuthenticationTokenAsync(
                user, OtpTokenProvider, OtpHashToken);

            await _userManager.RemoveAuthenticationTokenAsync(
                user, OtpTokenProvider, OtpExpiryToken);

            await _userManager.RemoveAuthenticationTokenAsync(
                user, OtpTokenProvider, OtpAttemptsToken);
        }

        private static bool TryParseUtc(
            string? value,
            out DateTime parsed)
        {
            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out parsed);
        }
    }
}