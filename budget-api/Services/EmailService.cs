using budget_api.Models.ViewModel;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace budget_api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(IEmailSender emailSender, ILogger<EmailService> logger, IConfiguration configuration)
        {
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
        }

    }
}
