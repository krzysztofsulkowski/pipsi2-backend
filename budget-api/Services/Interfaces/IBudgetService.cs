using budget_api.Models.DatabaseModels;
using budget_api.Models.Dto;
using budget_api.Models.ViewModel;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface IBudgetService
    {
        Task<ServiceResult> CreateBudgetAsync(BudgetViewModel model, string userId);
        Task<ServiceResult<BudgetViewModel>> GetBudgetByIdAsync(int budgetId, string userId);
        Task<ServiceResult> AcceptInvitationAsync(Guid token);
        Task<ServiceResult<InvitationResultData>> CreateInvitationAsync(int budgetId, string recipientEmail, string inviterUserId);
        Task<ServiceResult<DataTableResponse<BudgetDataTableDto>>> GetUserBudgetsDataTableAsync(string userId, DataTableRequest request);
        Task<ServiceResult> ArchiveBudgetAsync(int budgetId, string userId);
        Task<ServiceResult> UnarchiveBudgetAsync(int budgetId, string userId);
        Task<ServiceResult<List<BudgetSummaryViewModel>>> GetUserBudgetsAsync(string userId);
        Task<ServiceResult> EditBudgetAsync(int budgetId, EditBudgetViewModel model, string userId);
        Task<ServiceResult> RemoveMemberAsync(int budgetId, string targetUserId, string currentUserId);
        Task<ServiceResult<List<BudgetMemberDto>>> GetBudgetMembersAsync(int budgetId, string userId);
    }
}