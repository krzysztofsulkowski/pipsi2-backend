using budget_api.Models.ViewModel;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Produces("application/json")]
    public class DashboardController : BudgetApiBaseController
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger, IEmailService emailService)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Wysyła wiadomość kontaktową do administratora systemu.
        /// </summary>
        /// <remarks>
        /// Proces działania:
        /// 1. Wysyła wiadomość użytkownika na e-mail administratora.
        /// 2. Wysyła automatyczne potwierdzenie (auto-reply) na adres e-mail nadawcy podany w formularzu.
        /// </remarks>
        /// <param name="msg">Model zawierający imię, e-mail zwrotny oraz treść wiadomości.</param>
        /// <returns>Status operacji (Sukces lub informacja o błędzie wysyłki).</returns>
        [HttpPost("submit-message")]
        [ProducesResponseType(StatusCodes.Status200OK)] // E-mail wysłany pomyślnie
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Błąd walidacji danych wejściowych
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitMessage([FromBody] ContactMessageViewModel msg)
        {
            try
            {
                await _emailService.SendContactMessageToAdminAsync(msg);

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

