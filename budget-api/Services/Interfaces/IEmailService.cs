using budget_api.Models.ViewModel;

namespace budget_api.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendContactMessageToAdminAsync(ContactMessageViewModel message);
        Task SendContactConfirmationToUserAsync(ContactMessageViewModel message);
        Task SendInvitationAsync(string recipientEmail, string senderName, string budgetName, string invitationUrl);
    }
}
