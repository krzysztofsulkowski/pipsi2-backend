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
    [Produces("application/json")]
    public class AuthController : BudgetApiBaseController
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthController(IAuthService authService, SignInManager<IdentityUser> signInManager)
        {
            _authService = authService;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Rejestracja nowego użytkownika w systemie.
        /// </summary>
        /// <remarks>
        /// Sprawdza unikalność adresu e-mail i nazwy użytkownika. 
        /// Jeśli to pierwszy użytkownik w bazie, otrzymuje rolę Administratora, w przeciwnym razie - User.
        /// </remarks>
        /// <param name="registerDto">Model zawierający e-mail, nazwę użytkownika i hasło.</param>
        /// <returns>Status operacji (200 OK lub 400 z listą błędów).</returns>
        /// 
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Logowanie użytkownika przy użyciu adresu e-mail i hasła.
        /// </summary>
        /// <remarks>
        /// W przypadku sukcesu zwraca token JWT, który należy wysyłać w nagłówku "Authorization" (Bearer).
        /// </remarks>
        /// <param name="loginDto">Dane logowania (e-mail i hasło).</param>
        /// <returns>Obiekt zawierający token JWT.</returns>
        /// 
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.Login(loginDto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Inicjuje proces resetowania hasła.
        /// </summary>
        /// <remarks>
        /// Jeśli podany e-mail istnieje w bazie, system wysyła wiadomość z linkiem do resetu hasła.
        /// Metoda zwraca sukces (200) nawet jeśli e-mail nie istnieje (z powodów bezpieczeństwa, aby nie zdradzać bazy użytkowników).
        /// </remarks>
        /// <param name="forgotPasswordDto">Adres e-mail użytkownika.</param>
        /// 
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Ustawia nowe hasło na podstawie tokena resetującego.
        /// </summary>
        /// <param name="resetPasswordDto">Nowe hasło, email oraz token otrzymany w mailu.</param>
        /// <returns>Status operacji.</returns>
        /// 
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            return HandleServiceResult(result);
        }

        /// <summary>
        /// Rozpoczyna logowanie przez zewnętrznego dostawcę (OAuth).
        /// </summary>
        /// <remarks>
        /// Endpoint przekierowuje użytkownika na stronę logowania wybranego dostawcy
        /// </remarks>
        /// <param name="provider">Nazwa dostawcy (domyślnie "Facebook").</param>
        /// <param name="returnUrl">Opcjonalny URL powrotny po zalogowaniu.</param>
        /// 
        [HttpGet("external-login")]  //http://localhost:7128/api/authentication/external-login
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public IActionResult ExternalLogin(string provider = "Facebook", string returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }
        /// <summary>
        /// Callback obsługujący powrót użytkownika od zewnętrznego dostawcy.
        /// </summary>
        /// <remarks>
        /// Ten endpoint jest wywoływany automatycznie przez zewnętrznego providera. 
        /// Przetwarza dane logowania, tworzy konto (jeśli nie istnieje) i przekierowuje do Frontendu z tokenem JWT w parametrze URL.
        /// </remarks>
        /// <param name="returnUrl">URL, na który ma nastąpić przekierowanie (z tokenem).</param>
        [HttpGet("external-login-callback")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status302Found)]
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

        /// <summary>
        /// Pobiera informacje o aktualnie zalogowanym użytkowniku.
        /// </summary>
        /// <remarks>
        /// Wymaga przesłania poprawnego tokena JWT w nagłówku.
        /// </remarks>
        /// <returns>DTO z danymi użytkownika (np. nazwa, e-mail).</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)] 
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] 
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public async Task<IActionResult> GetMe()
        {
            var result = await _authService.GetMe();            
            return HandleServiceResult(result);
        }
    }
}
