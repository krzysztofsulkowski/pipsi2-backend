using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/payment-methods")]
    public class PaymentMethodController : BudgetApiBaseController
    {
        private readonly IPaymentMethodService _paymentMethodService;

        public PaymentMethodController(IPaymentMethodService paymentMethodService)
        {
            _paymentMethodService = paymentMethodService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPaymentMethods()
        {
            var result = await _paymentMethodService.GetAllPaymentMethodsAsync();
            return HandleServiceResult(result);
        }
    }
}
