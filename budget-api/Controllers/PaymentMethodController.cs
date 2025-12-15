using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/dictionaries")]
    public class PaymentMethodController : BudgetApiBaseController
    {
        private readonly IPaymentMethodService _paymentMethodService;

        public PaymentMethodController(IPaymentMethodService paymentMethodService)
        {
            _paymentMethodService = paymentMethodService;
        }

        /// <summary>
        /// Pobiera słownik dostępnych metod płatności.
        /// </summary>
        /// <remarks>
        /// Zwraca listę par: wartość enuma i wyświetlana nazwa polska (np. Cash -> Gotówka).
        /// </remarks>
        /// <returns>Lista metod płatności.</returns>
        [HttpGet] 
        [ProducesResponseType(typeof(List<PaymentMethodDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllPaymentMethods()
        {
            var result = await _paymentMethodService.GetAllPaymentMethodsAsync();
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera słownik dostępnych częstotliwości dla transakcji cyklicznych.
        /// </summary>
        /// <remarks>
        /// Służy do wypełniania list rozwijanych (select) w formularzach dodawania wydatków planowanych/cyklicznych.
        /// </remarks>
        /// <returns>Lista częstotliwości (np. Weekly, Monthly) z polskimi nazwami.</returns>
        [HttpGet("frequencies")] 
        [ProducesResponseType(typeof(List<FrequencyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllFrequencies()
        {
            var result = await _paymentMethodService.GetAllFrequenciesAsync();
            return HandleServiceResult(result);
        }
    }
}
