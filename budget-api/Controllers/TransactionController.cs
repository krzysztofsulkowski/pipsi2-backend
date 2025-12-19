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
    [Produces("application/json")]
    public class TransactionController : BudgetApiBaseController
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// Dodaje nowy przychód do wskazanego bud¿etu.
        /// </summary>
        /// <param name="budgetId">ID bud¿etu, do którego dodajemy przychód.</param>
        /// <param name="model">Dane nowego przychodu (kwota, tytu³, kategoria).</param>
        /// <returns>Status operacji.</returns>
        [HttpPost("income")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AddIncome([FromRoute] int budgetId, [FromBody] CreateIncomeDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;

            var result = await _transactionService.AddIncomeAsync(budgetId, model, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Wyszukuje transakcje (przychody i wydatki) w formacie tabelarycznym (Server-side rendering).
        /// </summary>
        /// <remarks>
        /// Obs³uguje paginacjê, sortowanie po kolumnach i wyszukiwanie tekstowe.
        /// Zwraca zarówno przychody, jak i wydatki.
        /// </remarks>
        /// <param name="budgetId">ID bud¿etu.</param>
        /// <param name="request">Parametry tabeli (Start, Length, Search, Order).</param>
        /// <returns>Obiekt z list¹ transakcji i metadanymi dla tabeli.</returns>
        [HttpPost("transactions/search")]
        [ProducesResponseType(typeof(DataTableResponse<TransactionListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchTransactions([FromRoute] int budgetId, [FromBody] DataTableRequest request)
        {
            var userId = CurrentUserId;

            var result = await _transactionService.SearchTransactionsAsync(budgetId, request, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera szczegó³y pojedynczego przychodu.
        /// </summary>
        /// <param name="budgetId">ID bud¿etu.</param>
        /// <param name="incomeId">ID transakcji (przychodu).</param>
        /// <returns>Szczegó³owe dane transakcji.</returns>
        [HttpGet("income/{incomeId:int}")]
        [ProducesResponseType(typeof(TransactionDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetIncome([FromRoute] int budgetId, [FromRoute] int incomeId)
        {
            var userId = CurrentUserId;

            var result = await _transactionService.GetIncomeDetailsAsync(budgetId, incomeId, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Edytuje istniej¹cy przychód.
        /// </summary>
        /// <param name="budgetId">ID bud¿etu.</param>
        /// <param name="incomeId">ID przychodu do edycji.</param>
        /// <param name="model">Zaktualizowane dane.</param>
        [HttpPost("income/{incomeId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditIncome([FromRoute] int budgetId, [FromRoute] int incomeId, [FromBody] CreateIncomeDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;

            var result = await _transactionService.EditIncomeAsync(budgetId, incomeId, model, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Usuwa przychód z bud¿etu.
        /// </summary>
        /// <remarks>
        /// Uwaga: Usuniêcie przychodu wp³ywa na ca³kowite saldo bud¿etu.
        /// </remarks>
        [HttpDelete("income/{incomeId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteIncome([FromRoute] int budgetId, [FromRoute] int incomeId)
        {
            var userId = CurrentUserId;

            var result = await _transactionService.DeleteIncomeAsync(budgetId, incomeId, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Dodaje nowy wydatek.
        /// </summary>
        /// <remarks>
        /// Metoda weryfikuje, czy w bud¿ecie s¹ wystarczaj¹ce œrodki. 
        /// Jeœli `Amount` > Saldo, zwracany jest b³¹d 400 (Bad Request).
        /// </remarks>
        /// <param name="budgetId">ID bud¿etu.</param>
        /// <param name="model">Dane wydatku (typ: natychmiastowy/cykliczny/planowany).</param>
        [HttpPost("expenses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddExpense([FromRoute] int budgetId, [FromBody] CreateExpenseDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;

            var result = await _transactionService.AddExpenseAsync(budgetId, model, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera szczegó³y pojedynczego wydatku.
        /// </summary>
        [HttpGet("expenses/{expenseId:int}")]
        [ProducesResponseType(typeof(TransactionDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetExpense([FromRoute] int budgetId, [FromRoute] int expenseId)
        {
            var userId = CurrentUserId;

            var result = await _transactionService.GetExpenseDetailsAsync(budgetId, expenseId, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Edytuje istniej¹cy wydatek.
        /// </summary>
        /// <remarks>
        /// Równie¿ sprawdza saldo. Jeœli zwiêkszenie kwoty wydatku spowodowa³oby ujemny bilans, operacja zostanie odrzucona.
        /// </remarks>
        [HttpPost("expenses/{expenseId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditExpense([FromRoute] int budgetId, [FromRoute] int expenseId, [FromBody] CreateExpenseDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = CurrentUserId;

            var result = await _transactionService.EditExpenseAsync(budgetId, expenseId, model, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Usuwa wydatek z historii.
        /// </summary>
        /// <remarks>
        /// Usuniêcie wydatku zwraca œrodki do salda bud¿etu.
        /// </remarks>
        [HttpDelete("expenses/{expenseId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteExpense([FromRoute] int budgetId, [FromRoute] int expenseId)
        {
            var userId = CurrentUserId;

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
