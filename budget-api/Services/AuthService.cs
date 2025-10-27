using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace budget_api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailSender _emailSender;

        public AuthService(UserManager<IdentityUser> userManager, ILogger<AuthService> logger, IConfiguration configuration, SignInManager<IdentityUser> signInManager, IWebHostEnvironment hostingEnvironment, IEmailSender emailSender)
        {
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _signInManager = signInManager;
            _hostingEnvironment = hostingEnvironment;
            _emailSender = emailSender;
        }

        public async Task<ServiceResult> RegisterAsync(RegisterDto registerDto, bool isAdmin = false)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return ServiceResult.Failure("User already exists!");
            }

            var userByUsername = await _userManager.FindByNameAsync(registerDto.Username);
            if (userByUsername != null)
            {
                return ServiceResult.Failure("Ta nazwa Uzytkownika jest już zajęta");
            }

            var user = new IdentityUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Username,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrator");

                if (admins.Count == 0)
                {
                    await _userManager.AddToRoleAsync(user, "Administrator");
                    _logger.LogInformation("First user created as an Administrator.");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("User created with User role.");
                }
                _logger.LogInformation("User created a new account with password.");
            }
            else
            {
                return ServiceResult.Failure("User creation failed!");
            }
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> CreateAdminUser(RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return ServiceResult.Failure("User already exists!");
            }

            var user = new IdentityUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Username,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Administrator");
                return ServiceResult.Success();
            }

            return ServiceResult.Failure("User creation failed!");
        }


        public async Task<ServiceResult<LoginResponse>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return ServiceResult<LoginResponse>.Failure("Invalid username or password");
            }

            var token = await GenerateJwtTokenAsync(user);

            return ServiceResult<LoginResponse>.Success(new LoginResponse(token));
        }

        private async Task<string> GenerateJwtTokenAsync(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var secret = _configuration["JWT:Secret"];
            var issuer = _configuration["JWT:Issuer"];
            var audience = _configuration["JWT:Audience"];

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                throw new InvalidOperationException("Konfiguracja JWT jest niekompletna.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(7);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);

            if (user == null)
            {
                return ServiceResult.Success();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var frontEndBaseUrl = _configuration["FRONTEND_BASE_URL"];
            if (string.IsNullOrEmpty(frontEndBaseUrl))
            {
                Console.WriteLine("OSTRZEŻENIE: Brak konfiguracji FRONTEND_BASE_URL. Używam domyślnego adresu 'http://localhost:3000' do wygenerowania linku resetowania hasła.");
                frontEndBaseUrl = "http://localhost:3000";
            }

            var encodedToken = WebUtility.UrlEncode(token);
            var encodedEmail = WebUtility.UrlEncode(user.Email);

            var callbackUrl = $"{frontEndBaseUrl}/reset-password?token={encodedToken}&email={encodedEmail}";

            var templatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Templates", "ForgotPasswordEmail.html");
            var emailBody = await File.ReadAllTextAsync(templatePath);

            emailBody = emailBody.Replace("{UserName}", user.UserName ?? "użytkowniku");
            emailBody = emailBody.Replace("{CallbackUrl}", callbackUrl);

            var subject = "Reset hasła";
            await _emailSender.SendEmailAsync(user.Email, subject, emailBody);
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return ServiceResult.Failure("Password reset failed.");
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (result.Succeeded)
            {
                return ServiceResult.Success();
            }

            return ServiceResult.Failure("Password reset failed.");
        }

        public async Task<ServiceResult<LoginResponse>> HandleExternalLoginAsync()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("Nie można załadować informacji od zewnętrznego dostawcy logowania.");
                return ServiceResult<LoginResponse>.Failure("Błąd podczas ładowania danych od zewnętrznego dostawcy.");
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("Użytkownik zalogowany przez {Name} provider.", info.LoginProvider);
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

                var token = await GenerateJwtTokenAsync(user);
                return ServiceResult<LoginResponse>.Success(new LoginResponse(token));
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Nie udało się pobrać adresu e-mail od dostawcy: {LoginProvider}", info.LoginProvider);
                    return ServiceResult<LoginResponse>.Failure("Nie udało się pobrać adresu e-mail od zewnętrznego dostawcy.");
                }

                var user = await _userManager.FindByEmailAsync(email);
                var isNewUser = user == null;

                if (isNewUser)
                {
                    user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var createUserResult = await _userManager.CreateAsync(user);
                    if (!createUserResult.Succeeded)
                    {
                        _logger.LogError("Nie udało się utworzyć użytkownika z emailem {Email}: {Errors}", email, string.Join(", ", createUserResult.Errors.Select(e => e.Description)));
                        return ServiceResult<LoginResponse>.Failure("Nie udało się utworzyć nowego użytkownika.");
                    }
                    _logger.LogInformation("Utworzono nowego użytkownika z emailem {Email} przez logowanie zewnętrzne.", email);

                    var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                    if (admins.Count == 0)
                    {
                        await _userManager.AddToRoleAsync(user, "Administrator");
                        _logger.LogInformation("Pierwszy użytkownik (zewnętrzny) został administratorem.");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        _logger.LogInformation("Nowy użytkownik (zewnętrzny) otrzymał rolę User.");
                    }
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    _logger.LogError("Nie udało się połączyć konta zewnętrznego dla użytkownika {Email}: {Errors}", email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                    return ServiceResult<LoginResponse>.Failure("Nie udało się powiązać konta zewnętrznego.");
                }

                _logger.LogInformation("Konto dla {Email} zostało powiązane z dostawcą {LoginProvider}", user.Email, info.LoginProvider);

                var finalToken = await GenerateJwtTokenAsync(user);
                return ServiceResult<LoginResponse>.Success(new LoginResponse(finalToken));
            }
        }
    }
}



