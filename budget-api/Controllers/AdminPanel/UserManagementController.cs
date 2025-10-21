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
    [Route("api/adminPanel/Uuers")]

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
            return HandleStatusCodeServiceResult(result);
        }

        [HttpPost("getAllUsers")]
        public async Task<IActionResult> GetAllUsers([FromBody] DataTableRequest request)
        {
            var serviceResponse = await _userManagementService.GetAllUsers(request);
            return HandleStatusCodeServiceResult(serviceResponse);
        }

        [HttpPost("createUser")]
        public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
        {
            var result = await _userManagementService.CreateUser(userDto, User);
            return HandleStatusCodeServiceResult(result);
        }

        [HttpPost("updateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UserDto user)
        {
            var result = await _userManagementService.UpdateUser(user);
            return HandleServiceResult(result);
        }

        [HttpPost("lockUser/{userId}")]
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

        [HttpPost("unlockUser/{userId}")]
        public async Task<IActionResult> UnlockUser([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy identyfikator użytkownika"));
            }

            var result = await _userManagementService.UnlockUser(userId);
            return HandleServiceResult(result);
        }

        [HttpGet("getUserById")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy parametr: ID użytkownika nie może być puste\r\n"));
            }

            var result = await _userManagementService.GetUserById(userId);
            return HandleStatusCodeServiceResult(result);
        }
    }
}




