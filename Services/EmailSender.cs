
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace PublicGrievanceSystem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            var senderEmail =
                _configuration["EmailSettings:SenderEmail"];

            var appPassword =
                _configuration["EmailSettings:AppPassword"];

            var host =
                _configuration["EmailSettings:Host"]
                ?? "smtp.gmail.com";

            var portValue =
                _configuration["EmailSettings:Port"];

            if (string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(appPassword))
            {
                throw new InvalidOperationException(
                    "Gmail sender settings are missing. Check .NET User Secrets.");
            }

            if (!int.TryParse(portValue, out var port))
            {
                port = 587;
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress("CivicTrace", senderEmail));

            message.To.Add(
                MailboxAddress.Parse(email));

            message.Subject = subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = htmlMessage
            }.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(
                host,
                port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                senderEmail,
                appPassword);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }
    }
}
