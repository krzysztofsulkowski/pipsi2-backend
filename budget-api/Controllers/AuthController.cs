using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Route("api/authentication")]
    public class AuthController : BudgetApiBaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return HandleServiceResult(result);
        }

        [HttpPost("create-admin-user")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateAdminUser([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto, isAdmin: true);
            return HandleServiceResult(result);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.Login(loginDto);
            return HandleStatusCodeServiceResult(result);
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            return HandleServiceResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            return HandleServiceResult(result);
        }
    }
}