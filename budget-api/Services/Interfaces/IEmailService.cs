using budget_api.Models.ViewModel;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface IEmailService
    {
        Task<ServiceResult> SendContactMessageToAdminAsync(ContactMessageViewModel message);
        Task<string?> SendInvitationAsync(string? senderName, string budgetName, string invitationUrl);
    }
}
