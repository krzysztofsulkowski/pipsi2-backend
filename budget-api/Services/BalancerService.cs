using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Services
{
    public class BalancerService
    {
        private readonly ILogger<BalancerService> _logger;

        public BalancerService(ILogger<BalancerService> logger)
        {
            _logger = logger;
        }

        public async Task<string?> GenerateEmailTemplateAsync(string? senderName, string budgetName, string invitationUrl)
        {
            string templateFileName = "SharedBudgetInvitationTemplate.html";
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