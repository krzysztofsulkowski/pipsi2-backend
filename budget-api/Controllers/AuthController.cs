using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sprache;

namespace budget_api.Controllers
{
    [ApiController]
    [Route("api/authentication")]
    public class AuthController : BudgetApiBaseController
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthController(IAuthService authService, SignInManager<IdentityUser> signInManager)
        {
            _authService = authService;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return HandleServiceResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.Login(loginDto);
            return HandleServiceResult(result);
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

        [HttpGet("external-login")]  //http://localhost:7128/api/authentication/external-login
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider = "Facebook", string returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        [HttpGet("external-login-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback([FromQuery] string returnUrl = null)
        {
            var result = await _authService.HandleExternalLoginAsync();

            if (result == null || string.IsNullOrEmpty(result.Data?.Token))
            {
                var errorUrl = returnUrl ?? "http://localhost:3000/login"; 
                return Redirect($"{errorUrl}?error=auth_failed");
            }

            var token = result.Data.Token;
            var successUrl = $"{returnUrl}?token={token}";
            return Redirect(successUrl);
        }

        [HttpGet("me")]
        [Authorize] 
        public async Task<IActionResult> GetMe()
        {
            var result = await _authService.GetMe();            
            return HandleServiceResult(result);
        }
    }
}
