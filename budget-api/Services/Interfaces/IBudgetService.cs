using budget_api.Models.DatabaseModels;
using budget_api.Models.ViewModel;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface IBudgetService
    {        
        Task<ServiceResult> CreateBudgetAsync(BudgetViewModel model, string userId);
        Task<ServiceResult<BudgetViewModel>> GetBudgetByIdAsync(int budgetId, string userId);
        Task<ServiceResult> AcceptInvitationAsync(Guid token);
        Task<ServiceResult<BudgetInvitation>> CreateInvitationAsync(int budgetId, string recipientEmail, string inviterUserId);
    }
}