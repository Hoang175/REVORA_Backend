using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using REVORA_BE.Services.Interfaces;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpServer"] ?? _configuration["EmailSettings:SmtpHost"];
                var smtpPortString = _configuration["EmailSettings:Port"] ?? _configuration["EmailSettings:SmtpPort"];
                var smtpUser = _configuration["EmailSettings:Username"] ?? _configuration["EmailSettings:SmtpUser"];
                var smtpPass = _configuration["EmailSettings:Password"] ?? _configuration["EmailSettings:SmtpPass"];

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogWarning("EmailSettings is not configured properly. Email to {To} was not sent. Content: {Body}", to, body);
                    return;
                }

                int smtpPort = 587;
                if (!string.IsNullOrEmpty(smtpPortString))
                {
                    int.TryParse(smtpPortString, out smtpPort);
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpUser, "REVORA System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Sent email to {To} with subject {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
            }
        }
    }
}
