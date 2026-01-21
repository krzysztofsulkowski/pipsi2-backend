using budget_api.Models.Dto;
using budget_api.Services.Results;
using System.Threading.Tasks;

namespace budget_api.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<ServiceResult<TransactionDetailsDto>> AddIncomeAsync(int budgetId, CreateIncomeDto model, string userId);
        Task<ServiceResult<TransactionDetailsDto>> GetIncomeDetailsAsync(int budgetId, int incomeId, string userId);
        Task<ServiceResult> EditIncomeAsync(int budgetId, int incomeId, CreateIncomeDto model, string userId);
        Task<ServiceResult> DeleteIncomeAsync(int budgetId, int incomeId, string userId);

        Task<ServiceResult> AddExpenseAsync(int budgetId, CreateExpenseDto model, string userId);
        Task<ServiceResult<TransactionDetailsDto>> GetExpenseDetailsAsync(int budgetId, int expenseId, string userId);
        Task<ServiceResult> EditExpenseAsync(int budgetId, int expenseId, CreateExpenseDto model, string userId);
        Task<ServiceResult> DeleteExpenseAsync(int budgetId, int expenseId, string userId);

        Task<ServiceResult<DataTableResponse<TransactionListItemDto>>> SearchTransactionsAsync(int budgetId, DataTableRequest request, string userId);
        Task<ServiceResult<List<TransactionListItemDto>>> GetTransactionsForStatsAsync(int budgetId, int? year, int month);

        Task<ServiceResult> ProcessRecurringAndPlannedExpensesAsync();
    }
}
