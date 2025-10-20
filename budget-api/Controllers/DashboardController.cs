using budget_api.Models.ViewModel;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    public class DashboardController : BudgetApiBaseController
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger, IEmailService emailService)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("SubmitMessage")]
        public async Task<IActionResult> SubmitMessage([FromBody] ContactMessageViewModel msg)
        {
            try
            {
                await _emailService.SendContactMessageToAdminAsync(msg);
                await _emailService.SendContactConfirmationToUserAsync(msg);

                return HandleServiceResult(ServiceResult.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania wiadomości kontaktowej z HomeController");

                var result = ServiceResult.Failure("Wystąpił błąd podczas wysyłania wiadomości. Spróbuj ponownie później.");
                return HandleServiceResult(result);
            }
        }
    }
}