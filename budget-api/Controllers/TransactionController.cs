using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/budget/{budgetId:int}")]
    public class TransactionController : BudgetApiBaseController
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Add income
        [HttpPost("income")]
        public async Task<IActionResult> AddIncome([FromRoute] int budgetId, [FromBody] CreateIncomeDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.AddIncomeAsync(budgetId, model, userId);
            return HandleServiceResult(result);
        }

        // Search transactions (DataTable)
        [HttpPost("transactions/search")]
        public async Task<IActionResult> SearchTransactions([FromRoute] int budgetId, [FromBody] DataTableRequest request)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.SearchTransactionsAsync(budgetId, request, userId);
            return HandleServiceResult(result);
        }

        // Get income details
        [HttpGet("income/{incomeId:int}")]
        public async Task<IActionResult> GetIncome([FromRoute] int budgetId, [FromRoute] int incomeId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.GetIncomeDetailsAsync(budgetId, incomeId, userId);
            return HandleServiceResult(result);
        }

        // Edit income
        [HttpPost("income/{incomeId:int}")]
        public async Task<IActionResult> EditIncome([FromRoute] int budgetId, [FromRoute] int incomeId, [FromBody] CreateIncomeDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.EditIncomeAsync(budgetId, incomeId, model, userId);
            return HandleServiceResult(result);
        }

        // Delete income
        [HttpDelete("income/{incomeId:int}")]
        public async Task<IActionResult> DeleteIncome([FromRoute] int budgetId, [FromRoute] int incomeId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.DeleteIncomeAsync(budgetId, incomeId, userId);
            return HandleServiceResult(result);
        }

        // Add expense
        [HttpPost("expenses")]
        public async Task<IActionResult> AddExpense([FromRoute] int budgetId, [FromBody] CreateExpenseDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.AddExpenseAsync(budgetId, model, userId);
            return HandleServiceResult(result);
        }

        // Get expense details
        [HttpGet("expenses/{expenseId:int}")]
        public async Task<IActionResult> GetExpense([FromRoute] int budgetId, [FromRoute] int expenseId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.GetExpenseDetailsAsync(budgetId, expenseId, userId);
            return HandleServiceResult(result);
        }

        // Edit expense
        [HttpPost("expenses/{expenseId:int}")]
        public async Task<IActionResult> EditExpense([FromRoute] int budgetId, [FromRoute] int expenseId, [FromBody] CreateExpenseDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.EditExpenseAsync(budgetId, expenseId, model, userId);
            return HandleServiceResult(result);
        }

        // Delete expense
        [HttpDelete("expenses/{expenseId:int}")]
        public async Task<IActionResult> DeleteExpense([FromRoute] int budgetId, [FromRoute] int expenseId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _transactionService.DeleteExpenseAsync(budgetId, expenseId, userId);
            return HandleServiceResult(result);
        }

        //[HttpGet("run-recurrent-job")]  //do testów
        //public async Task<IActionResult> RunRecurrentJobTest() 
        //{
        //    var result = await _transactionService.ProcessRecurringAndPlannedExpensesAsync();

        //    return HandleServiceResult(result);
        //}
    }
}
