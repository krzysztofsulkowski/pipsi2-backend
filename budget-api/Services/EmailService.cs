using budget_api.Models.ViewModel;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
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

        public async Task<ServiceResult> SendContactMessageToAdminAsync(ContactMessageViewModel message)
        {
            try
            {
                string subject = "Zapytanie ze strony Balancer";

                string adminEmail = _configuration["EmailConfiguration:From"];
                if (string.IsNullOrEmpty(adminEmail))
                {
                    _logger.LogError("Adres email administratora (EmailConfiguration:From) nie jest ustawiony w konfiguracji.");
                    return ServiceResult.Failure("Błąd konfiguracji serwera. Nie można wysłać wiadomości.");
                }

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

                await SendContactConfirmationToUserAsync(message, subject);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania wiadomości kontaktowej");
                return ServiceResult.Failure("Wystąpił błąd podczas wysyłania wiadomości. Spróbuj ponownie później.");
            }
        }


        private async Task SendContactConfirmationToUserAsync(ContactMessageViewModel message, string subject)
        {
            try
            {
                string confirmationSubject = "Potwierdzenie otrzymania wiadomości - Balancer";
                string confirmationBody = $@"
                    <h2>Dziękujemy za kontakt!</h2>
                    <p>Otrzymaliśmy Twoją wiadomość i odpowiemy na nią jak najszybciej.</p>
                    <p><strong>Temat:</strong> {subject}</p>
                    <p><strong>Treść Twojej wiadomości:</strong></p>
                    <blockquote>{message.Message}</blockquote>
                    <hr>
                    <p>Z wyrazami szacunku,<br>Zespół Balancer</p>
                ";

                await _emailSender.SendEmailAsync(message.Email, confirmationSubject, confirmationBody);
                _logger.LogInformation("Potwierdzenie wysłania wiadomości zostało wysłane do {UserEmail}.", message.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się wysłać emaila z potwierdzeniem do {UserEmail}.", message.Email);
            }
        }


        public async Task<ServiceResult> SendBudgetInvitationAsync(string senderName, string recipientEmail, string budgetName, string invitationUrl, bool userExists)
        {
            try
            {
                string subject = $"{senderName} zaprasza Cię do wspólnego budżetu '{budgetName}'";
                string templateFileName = userExists
                    ? "SharedBudgetInvitationExistingUser.html"
                    : "SharedBudgetInvitationNewUser.html";

                string? emailBody = await CreateInvitationBodyAsync(senderName, budgetName, invitationUrl, templateFileName);

                if (emailBody == null)
                {
                    return ServiceResult.Failure("Nie udało się utworzyć treści e-maila z zaproszeniem z powodu błędu szablonu.");
                }

                await _emailSender.SendEmailAsync(recipientEmail, subject, emailBody);

                _logger.LogInformation("Zaproszenie do budżetu '{budgetName}' zostało wysłane do {recipientEmail}", budgetName, recipientEmail);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania zaproszenia do budżetu dla {recipientEmail}", recipientEmail);
                return ServiceResult.Failure("Wystąpił nieoczekiwany błąd podczas wysyłania zaproszenia.");
            }
        }

        private async Task<string?> CreateInvitationBodyAsync(string? senderName, string budgetName, string invitationUrl, string templateFileName)
        {
            string templateDirectory = Path.Combine(AppContext.BaseDirectory, "Templates");
            string templatePath = Path.Combine(templateDirectory, templateFileName);

            string emailTemplateContent;
            try
            {
                emailTemplateContent = await File.ReadAllTextAsync(templatePath);
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogError("Nie znaleziono folderu szablonów email: {TemplateDirectory}", templateDirectory);
                return null;
            }
            catch (FileNotFoundException)
            {
                _logger.LogError("Nie znaleziono pliku szablonu email: {TemplatePath}", templatePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas odczytu szablonu email z pliku: {TemplatePath}", templatePath);
                return null;
            }

            emailTemplateContent = emailTemplateContent.Replace("{InviterName}", senderName ?? "Znajomy");
            emailTemplateContent = emailTemplateContent.Replace("{BudgetName}", budgetName);
            emailTemplateContent = emailTemplateContent.Replace("{InvitationUrl}", invitationUrl);

            return emailTemplateContent;
        }
    }
}