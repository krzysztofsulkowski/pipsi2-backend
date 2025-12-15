using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ReportsController : BudgetApiBaseController
    {
        private readonly ITransactionService _transactionService;

        public ReportsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Pobiera listę transakcji do generowania wykresów i statystyk.
        /// </summary>
        /// <remarks>
        /// Służy do pobierania surowych danych, które frontend przetwarza na wykresy (słupkowe/kołowe).
        /// </remarks>
        /// <param name="year">Rok, za który mają zostać pobrane dane (wymagane).</param>
        /// <param name="month">Numer miesiąca (1-12). Podanie wartości 0 spowoduje pobranie danych za cały rok.</param>
        /// <param name="budgetId">ID budżetu. Jeśli nie zostanie podane, API zwróci pustą listę (status 200).</param>
        /// <returns>Lista prostych obiektów transakcji (data, kwota, kategoria, typ).</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(List<TransactionListItemDto>), StatusCodes.Status200OK)] 
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

