using budget_api.Models.Dto;
using budget_api.Models.ViewModel;
using budget_api.Services;
using budget_api.Services.Errors;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/budget")]
    [Produces("application/json")]
    public class BudgetController : BudgetApiBaseController
    {
        private readonly ILogger<BudgetController> _logger;
        private readonly IEmailService _emailService;
        private readonly IBudgetService _budgetService;

        public BudgetController(ILogger<BudgetController> logger, IEmailService emailService, IBudgetService budgetService)
        {
            _emailService = emailService;
            _logger = logger;
            _budgetService = budgetService;
        }


        /// <summary>
        /// Wysyła e-mail z zaproszeniem do współdzielenia budżetu.
        /// </summary>
        /// <remarks>
        /// Generuje token zaproszenia i wysyła link na podany adres e-mail. 
        /// Jeśli zaproszony użytkownik istnieje, link od razu doda go do budżetu po kliknięciu.
        /// Jeśli nie istnieje, link poprosi o rejestrację.
        /// </remarks>
        /// <param name="model">Zawiera ID budżetu, nazwę budżetu i email odbiorcy.</param>
        /// <returns>Status wysłania wiadomości email.</returns>
        [HttpPost("send-invitation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendInvitation([FromBody] SendInvitationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var inviterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var invitationResult = await _budgetService.CreateInvitationAsync(model.BudgetId, model.RecipientEmail, inviterUserId);

                if (!invitationResult.IsSuccess)
                {
                    return BadRequest();
                }

                var invitationUrl = Url.Action(
                   "AcceptInvitation",
                   "Budget",
                   new { token = invitationResult.Data.Invitation.Token },
                   Request.Scheme
               );

                if (string.IsNullOrEmpty(invitationUrl))
                {
                    _logger.LogError("Nie udało się wygenerować adresu URL zaproszenia. Sprawdź, czy routing jest poprawnie skonfigurowany.");
                    return StatusCode(500, new { message = "Wystąpił wewnętrzny błąd serwera podczas generowania linku." });
                }

                string senderName = User.Identity?.Name ?? "Twój znajomy";
                var emailResult = await _emailService.SendBudgetInvitationAsync(senderName, model.RecipientEmail, model.BudgetName, invitationUrl, invitationResult.Data.UserExistsInSystem);

                return HandleServiceResult(emailResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił nieoczekiwany błąd podczas wysyłania zaproszenia.");
                return StatusCode(500, new { message = "Wystąpił nieoczekiwany błąd." });
            }
        }

        /// <summary>
        /// Przetwarza kliknięcie w link zapraszający.
        /// </summary>
        /// <remarks>
        /// Jeśli użytkownik istnieje - dodaje go do budżetu (Role: Member).
        /// Jeśli użytkownik nie istnieje - zwraca 401 z informacją `actionRequired: register`.
        /// </remarks>
        /// <param name="token">Token GUID z linku w emailu.</param>
        [HttpGet("accept-invitation")]
        [AllowAnonymous] 
        [ProducesResponseType(StatusCodes.Status200OK)] 
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        public async Task<IActionResult> AcceptInvitation([FromQuery] Guid token)
        {
            if (token == Guid.Empty)
            {
                return BadRequest(new { message = "Nieprawidłowy token." });
            }
            var result = await _budgetService.AcceptInvitationAsync(token);
            if (!result.IsSuccess)
            {
                var registrationError = InvitationError.UserRegistrationRequiredError();
                if (result.Error.Code == registrationError.Code)
                {
                    return StatusCode(401, new
                    {
                        actionRequired = "register",
                        message = result.Error.Description,
                        invitationToken = token
                    });
                }
            }
            return HandleServiceResult(result);
        }


        /// <summary>
        /// Tworzy nowy budżet.
        /// </summary>
        /// <remarks>
        /// Użytkownik tworzący budżet automatycznie staje się jego właścicielem (Owner).
        /// </remarks>
        /// <param name="model">Nazwa nowego budżetu.</param>
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateBudget([FromBody] BudgetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.CreateBudgetAsync(model, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera szczegóły pojedynczego budżetu.
        /// </summary>
        /// <param name="budgetId">ID budżetu.</param>
        /// <returns>Model z nazwą i ID budżetu.</returns>
        [HttpGet("{budgetId:int}")]
        [ProducesResponseType(typeof(BudgetViewModel), StatusCodes.Status200OK)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBudgetById(int budgetId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.GetBudgetByIdAsync(budgetId, userId);

            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera listę wszystkich budżetów, do których użytkownik ma dostęp.
        /// </summary>
        /// <remarks>
        /// Zawiera zarówno budżety stworzone przez użytkownika, jak i te, do których został zaproszony.
        /// </remarks>
        /// <returns>Lista podsumowań budżetów (rola, status, data).</returns>
        [HttpGet("my-budgets")]
        [ProducesResponseType(typeof(List<BudgetSummaryViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyBudgets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.GetUserBudgetsAsync(userId);
            return HandleServiceResult(result);
        }
        
        [HttpPost("my-budgets-datatable")]
        public async Task<IActionResult> GetMyBudgetsDataTable([FromBody] DataTableRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.GetUserBudgetsDataTableAsync(userId, request);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// Edytuje nazwę budżetu.
        /// </summary>
        /// <remarks>
        /// Operacja dostępna tylko dla Właściciela (Owner) budżetu.
        /// </remarks>
        /// <param name="budgetId">ID budżetu do edycji.</param>
        /// <param name="model">Nowe dane (nazwa).</param>
        [HttpPost("{budgetId:int}/edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EditBudget([FromRoute] int budgetId, [FromBody] EditBudgetViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.EditBudgetAsync(budgetId, model, userId);
            return HandleServiceResult(result);
        }

        [HttpGet("{budgetId:int}/members")]
        public async Task<IActionResult> GetBudgetMembers([FromRoute] int budgetId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.GetBudgetMembersAsync(budgetId, userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Usuwa użytkownika z budżetu lub opuszcza budżet.
        /// </summary>
        /// <remarks>
        /// - Właściciel może usunąć członka, ale nie innego właściciela.
        /// - Właściciel nie może usunąć samego siebie (musi usunąć budżet).
        /// - Zwykły członek może opuścić budżet (usuwając siebie).
        /// </remarks>
        /// <param name="budgetId">ID budżetu.</param>
        /// <param name="userId">ID użytkownika do usunięcia (targetUserId).</param>
        [HttpDelete("{budgetId:int}/members/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveMember([FromRoute] int budgetId, [FromRoute] string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.RemoveMemberAsync(budgetId, userId, currentUserId);
            return HandleServiceResult(result);
        }


        /// <summary>
        /// Archiwizuje budżet (Soft delete).
        /// </summary>
        /// <remarks>
        /// Dostępne tylko dla Właściciela. Budżet zarchiwizowany jest "tylko do odczytu".
        /// </remarks>
        [HttpPost("{budgetId:int}/archive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ArchiveBudget([FromRoute] int budgetId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.ArchiveBudgetAsync(budgetId, userId);
            return HandleServiceResult(result);
        }
        /// <summary>
        /// Przywraca zarchiwizowany budżet do aktywności.
        /// </summary>
        /// <remarks>
        /// Dostępne tylko dla Właściciela.
        /// </remarks>
        [HttpPost("{budgetId:int}/unarchive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnarchiveBudget([FromRoute] int budgetId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _budgetService.UnarchiveBudgetAsync(budgetId, userId);
            return HandleServiceResult(result);
        }
    }
}