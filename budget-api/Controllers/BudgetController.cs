﻿using budget_api.Models.ViewModel;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using budget_api.Services.Errors;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/budget")]
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


        [HttpPost("send-invitation")]
        public async Task<IActionResult> SendInvitation([FromBody] SendInvitationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var inviterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(inviterUserId))
                {
                    return Unauthorized(new { message = "Nie można zidentyfikować użytkownika wysyłającego zaproszenie." });
                }

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

        [HttpGet("accept-invitation")]
        [AllowAnonymous]
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


        [HttpPost("create")]
        public async Task<IActionResult> CreateBudget([FromBody] BudgetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Nie można zidentyfikować użytkownika." });
            }

            var result = await _budgetService.CreateBudgetAsync(model, userId);
            return HandleServiceResult(result);
        }

        [HttpGet("{budgetId:int}")]
        public async Task<IActionResult> GetBudgetById(int budgetId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _budgetService.GetBudgetByIdAsync(budgetId, userId);

            return HandleStatusCodeServiceResult(result);
        }
    }
}