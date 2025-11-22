using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace budget_api.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var apiKey = _config["EmailConfiguration:ApiKey"];
            var fromAddress = _config["EmailConfiguration:From"];

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(fromAddress, "BALANCR");
            var to = new EmailAddress(email);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                "",
                htmlMessage
            );

            await client.SendEmailAsync(msg);
        }
    }
}
