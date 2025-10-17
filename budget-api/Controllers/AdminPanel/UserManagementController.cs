using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using budget_api.Models;
using budget_api.Models.Dto;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using budget_api.Services.Interfaces;

namespace budget_api.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "Administrator")]
    [Route("AdminPanel/Users")]
    public class UserManagementController : BudgetApiBaseController
    {
        private readonly IUserManagementService _userManagementService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(IUserManagementService userManagementService, RoleManager<IdentityRole> roleManager)
        {
            _userManagementService = userManagementService;
            _roleManager = roleManager;
        }

        //[HttpGet("Roles")]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //public async Task<IActionResult> GetAllRoles()
        //{
        //    var roles = await _roleManager.Roles.ToListAsync();
        //    return HandleServiceResult(roles);
        //}

        [HttpPost("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers([FromForm] DataTableRequest request)
        {
            var serviceResponse = await _userManagementService.GetAllUsers(request);
            return HandleStatusCodeServiceResult(serviceResponse);
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromForm] UserDto user)
        {
            var result = await _userManagementService.CreateUser(user);
            return HandleStatusCodeServiceResult(result);
        }

        [HttpPost("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromForm] UserDto user)
        {
            var result = await _userManagementService.UpdateUser(user);
            return HandleServiceResult(result);
        }

        [HttpPost("LockUser/{userId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
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

        [HttpPost("UnlockUser/{userId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UnlockUser([FromRoute] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ServiceResult.Failure("Nieprawidłowy identyfikator użytkownika"));
            }

            var result = await _userManagementService.UnlockUser(userId);
            return HandleServiceResult(result);
        }

        [HttpGet("GetUserById")]
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




