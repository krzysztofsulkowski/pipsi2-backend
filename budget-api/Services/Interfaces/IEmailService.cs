using budget_api.Models.ViewModel;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface IEmailService
    {
        Task<ServiceResult> SendContactMessageToAdminAsync(ContactMessageViewModel message);
        Task<ServiceResult> SendBudgetInvitationAsync(string senderName, string recipientEmail, string budgetName, string invitationUrl, bool userExists);
        Task<ServiceResult> SendRecurrentExpenseSuccessNotificationAsync(string recipientEmail, string budgetName, string transactionTitle, decimal amount);
        Task<ServiceResult> SendRecurrentExpenseFailedNotificationAsync(string recipientEmail, string budgetName, string transactionTitle, decimal amount, string reason);
    }
}