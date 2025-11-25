using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace budget_api.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [Route("api/adminPanel/users")]

    public class UserManagementController : BudgetApiBaseController
    {
        private readonly IUserManagementService _userManagementService;
        
        public UserManagementController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _userManagementService.GetAllRolesAsync();
            return HandleServiceResult(result);
        }

        [HttpPost("get-all-users")]
        public async Task<IActionResult> GetAllUsers([FromBody] DataTableRequest request)
        {
            var serviceResponse = await _userManagementService.GetAllUsers(request);
            return HandleServiceResult(serviceResponse);
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
        {
            var result = await _userManagementService.CreateUser(userDto, User);
            return HandleServiceResult(result);
        }

        [HttpPost("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UserDto user)
        {
            var result = await _userManagementService.UpdateUser(user);
            return HandleServiceResult(result);
        }

        [HttpPost("lock-user/{userId}")]
        public async Task<IActionResult> LockUser([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy identyfikator użytkownika"));
            }

            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != null && userId == currentUserId)
            {
                return BadRequest(ServiceResult.Failure("Nie można zablokować własnego konta"));
            }

            var result = await _userManagementService.LockUser(userId);
            return HandleServiceResult(result);
        }

        [HttpPost("unlock-user/{userId}")]
        public async Task<IActionResult> UnlockUser([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy identyfikator użytkownika"));
            }

            var result = await _userManagementService.UnlockUser(userId);
            return HandleServiceResult(result);
        }

        [HttpGet("get-user-by-id")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy parametr: ID użytkownika nie może być puste\r\n"));
            }

            var result = await _userManagementService.GetUserById(userId);
            return HandleServiceResult(result);
        }
    }
}




