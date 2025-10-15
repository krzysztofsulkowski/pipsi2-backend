using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace budget_api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<IdentityUser> userManager, ILogger<AuthService> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ServiceResult> RegisterAsync(RegisterDto registerDto, bool isAdmin = false)
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
                return ServiceResult<LoginResponse>.Failure( "Invalid username or password");
            }

            var token = GenerateJwtToken(user);

            return ServiceResult<LoginResponse>.Success(new LoginResponse(token));
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var secret = _configuration["JWT:Secret"];
            var issuer = _configuration["JWT:Issuer"];
            var audience = _configuration["JWT:Audience"];

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                throw new InvalidOperationException("JWT configuration is missing in app settings.");

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

            //  var callbackUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={token}&email={user.Email}";
            // await _emailSender.SendEmailAsync(user.Email, "Reset Password", $"Please reset your password by clicking here: {callbackUrl}");

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
    }
}