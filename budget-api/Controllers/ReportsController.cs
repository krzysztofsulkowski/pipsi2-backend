using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ReportsController : BudgetApiBaseController
    {
        private readonly ITransactionService _transactionService;

        public ReportsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int year, [FromQuery] int month, [FromQuery] int? budgetId)
        {

            int finalBudgetId = budgetId ?? 0;

            if (finalBudgetId == 0)
            {
                return Ok(new List<TransactionListItemDto>());
            }
            var transactions = await _transactionService.GetTransactionsForStatsAsync(finalBudgetId, year, month);
            return HandleServiceResult(transactions);
        }
    }
}

