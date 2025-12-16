using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using budget_api.Services.Errors; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace budget_api.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [Route("api/adminPanel/users")]
    [Produces("application/json")]
    public class UserManagementController : BudgetApiBaseController
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagementController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        /// <summary>
        /// Pobiera listę wszystkich dostępnych ról w systemie (np. User, Administrator).
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(List<IdentityRole>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _userManagementService.GetAllRolesAsync();
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera listę użytkowników w formacie tabelarycznym (Server-side rendering).
        /// </summary>
        /// <param name="request">Parametry tabeli (wyszukiwanie, sortowanie, stronicowanie).</param>
        [HttpPost("get-all-users")]
        [ProducesResponseType(typeof(DataTableResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers([FromBody] DataTableRequest request)
        {
            var serviceResponse = await _userManagementService.GetAllUsers(request);
            return HandleServiceResult(serviceResponse);
        }

        /// <summary>
        /// Tworzy nowego użytkownika administracyjnie.
        /// </summary>
        /// <remarks>
        /// Domyślnie ustawia hasło 'DefaultPassword123!'. Przypisuje wskazaną rolę.
        /// Zwykły administrator nie może utworzyć super-admina, chyba że jest to jawnie zablokowane logiką serwisu.
        /// </remarks>
        [HttpPost("create-user")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
        {
            var result = await _userManagementService.CreateUser(userDto, User);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Aktualizuje dane istniejącego użytkownika (Nazwa, Email, Rola).
        /// </summary>
        [HttpPost("update-user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser([FromBody] UserDto user)
        {
            var result = await _userManagementService.UpdateUser(user);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Blokuje użytkownika na stałe (Lockout).
        /// </summary>
        /// <remarks>
        /// Nie można zablokować własnego konta.
        /// </remarks>
        [HttpPost("lock-user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> LockUser([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                var error = CommonErrors.BadRequest("Nieprawidłowy identyfikator użytkownika");
                return BadRequest(ServiceResult.Failure(error.Description));
            }

            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != null && userId == currentUserId)
            {
                // Jawnie zwracamy specyficzny błąd
                return BadRequest(ServiceResult.Failure("Nie można zablokować własnego konta"));
            }

            var result = await _userManagementService.LockUser(userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Odblokowuje użytkownika.
        /// </summary>
        [HttpPost("unlock-user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UnlockUser([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy identyfikator użytkownika"));
            }

            var result = await _userManagementService.UnlockUser(userId);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Pobiera szczegóły pojedynczego użytkownika (wraz z rolą).
        /// </summary>
        [HttpGet("get-user-by-id")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserById([FromQuery] string userId) 
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy parametr: ID użytkownika nie może być puste."));
            }

            var result = await _userManagementService.GetUserById(userId);
            return HandleServiceResult(result);
        }
    }
}



