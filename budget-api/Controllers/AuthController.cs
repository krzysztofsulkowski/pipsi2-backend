using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace budget_api.Controllers
{
    public record RegisterDto([Required] string Email, [Required] string Username, [Required] string Password);
    public record LoginDto([Required] string Email, [Required] string Password);
    public record LoginResponse(string Token);


    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterDto> _logger;

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration, ILogger<RegisterDto> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return BadRequest(new { Message = "User already exists!" });
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
                return BadRequest(new { Message = "User creation failed!", Errors = result.Errors });
            }

            return Ok(new { Message = "User created successfully!" });
        }

        [HttpPost("create-admin-user")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateAdminUser([FromBody] RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return BadRequest(new { Message = "User already exists!" });
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
                return Ok(new { Message = "Administrator user created successfully!" });
            }

            return BadRequest(new { Message = "User creation failed!", Errors = result.Errors });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return Unauthorized(new { Message = "Invalid username or password" });
            }

            var token = GenerateJwtToken(user);

            return Ok(new LoginResponse(token));
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
    }

}






