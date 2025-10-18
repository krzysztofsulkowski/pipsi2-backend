using budget_api.Models;
using budget_api.Models.ViewModel;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace budget_api.Services
{
    public class ContactService
    {
        private readonly BudgetApiDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ContactService> _logger;
        private readonly IConfiguration _configuration;

        public ContactService(
            BudgetApiDbContext context,
            IEmailSender emailSender,
            ILogger<ContactService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ServiceResult> SubmitMessage(ContactMessageViewModel message)
        {
            try
            {
                string subject = "Zapytanie ze strony Balancer";

                string adminEmail = _configuration["EmailConfiguration:From"];
                string emailSubject = $"Nowa wiadomość kontaktowa: {subject}";
                string emailBody = $@"
                    <h2>Nowa wiadomość kontaktowa</h2>
                    <p><strong>Od:</strong> {message.Name} ({message.Email})</p>
                    <p><strong>Temat:</strong> {subject}</p>
                    <p><strong>Wiadomość:</strong></p>
                    <p>{message.Message}</p>
                    <hr>
                    <p>Ta wiadomość została wysłana z formularza kontaktowego Balancer.</p>
                ";

                await _emailSender.SendEmailAsync(adminEmail, emailSubject, emailBody);

                // potwierdzenie do nadawcy
                string confirmationSubject = "Potwierdzenie otrzymania wiadomości - Balancer";
                string confirmationBody = $@"
                    <h2>Dziękujemy za kontakt!</h2>
                    <p>Otrzymaliśmy Twoją wiadomość i odpowiemy na nią jak najszybciej.</p>
                    <p><strong>Temat:</strong> {subject}</p>
                    <p><strong>Treść Twojej wiadomości:</strong></p>
                    <p>{message.Message}</p>
                    <hr>
                    <p>Z wyrazami szacunku,<br>Zespół Balancer</p>
                ";

                await _emailSender.SendEmailAsync(message.Email, confirmationSubject, confirmationBody);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania wiadomości kontaktowej");
                return ServiceResult.Failure("Wystąpił błąd podczas wysyłania wiadomości. Spróbuj ponownie później.");
            }
        }
    }
}