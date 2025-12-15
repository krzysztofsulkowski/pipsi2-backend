using budget_api.Models;
using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using budget_api.Services.Errors; // Import błędów

namespace budget_api.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly BudgetApiDbContext _context;
        private readonly ILogger<UserManagementService> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private const string ObjectName = "User";

        public UserManagementService(BudgetApiDbContext context, ILogger<UserManagementService> logger, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ServiceResult<List<IdentityRole>>> GetAllRolesAsync()
        {
            try
            {
                var roles = await _roleManager.Roles.ToListAsync();
                return ServiceResult<List<IdentityRole>>.Success(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas pobierania ról.");
                return ServiceResult<List<IdentityRole>>.Failure(CommonErrors.FetchFailed("Roles"));
            }
        }

        public async Task<ServiceResult<DataTableResponse<UserDto>>> GetAllUsers(DataTableRequest request)
        {
            try
            {
                string[] columnNames = { "UserName", "Email", "Role" };
                string sortColumn = (request.OrderColumn >= 0 && request.OrderColumn < columnNames.Length) ? columnNames[request.OrderColumn] : "UserName";
                string sortDirection = request.OrderDir?.ToUpper() == "DESC" ? "DESC" : "ASC";

                var filteredUsers = await _context.Set<UserDto>()
                    .FromSqlInterpolated($"SELECT * FROM public.get_users_with_roles({request.SearchValue ?? (object)DBNull.Value}, {sortColumn}, {sortDirection})")
                    .ToListAsync();

                int totalRecords = await _context.Users.CountAsync();
                int filteredRecords = filteredUsers.Count;

                return ServiceResult<DataTableResponse<UserDto>>.Success(
                    new DataTableResponse<UserDto>
                    {
                        Draw = request.Draw,
                        RecordsTotal = totalRecords,
                        RecordsFiltered = filteredRecords,
                        Data = filteredUsers
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users with roles: {ErrorDetails}", ex.ToString());
                return ServiceResult<DataTableResponse<UserDto>>.Failure(CommonErrors.DataProcessingError("Błąd pobierania listy użytkowników."));
            }
        }


        public async Task<ServiceResult> LockUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, userId));

                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); 

                _logger.LogInformation("User {UserId} locked permanently", userId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently locking user {UserId}", userId);
                return ServiceResult.Failure(CommonErrors.UpdateFailed(ObjectName, userId, "Błąd blokowania konta."));
            }
        }

        public async Task<ServiceResult> UnlockUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, userId));

                await _userManager.SetLockoutEndDateAsync(user, null);

                _logger.LogInformation("User {UserId} unlocked", userId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user {UserId}", userId);
                return ServiceResult.Failure(CommonErrors.UpdateFailed(ObjectName, userId, "Błąd odblokowania konta."));
            }
        }

        public async Task<ServiceResult<UserDto>> CreateUser(UserDto user, ClaimsPrincipal currentUser)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return ServiceResult<UserDto>.Failure(AuthErrors.EmailRequired());
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUserByEmail = await _userManager.FindByEmailAsync(user.Email);
                if (existingUserByEmail != null)
                    return ServiceResult<UserDto>.Failure(AuthErrors.UserAlreadyExists());

                var role = await _context.Roles.FindAsync(user.RoleId);
                if (role == null)
                    return ServiceResult<UserDto>.Failure(UserManagementErrors.RoleNotFound(user.RoleId));

                if (string.IsNullOrEmpty(role.Name))
                    return ServiceResult<UserDto>.Failure(UserManagementErrors.RoleNotFound(user.RoleId)); 

                if (role.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentUser == null || !currentUser.IsInRole("Administrator"))
                    {
                        _logger.LogWarning("Użytkownik bez uprawnień próbował utworzyć admina.");
                        return ServiceResult<UserDto>.Failure(UserManagementErrors.CannotCreateAdminWithoutPrivileges());
                    }
                }

                var newUser = new IdentityUser
                {
                    Email = user.Email,
                    UserName = user.UserName,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(newUser, "DefaultPassword123!");
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("CreateAsync failed: {Errors}", errors);
                    return ServiceResult<UserDto>.Failure(UserManagementErrors.CreateFailed(errors));
                }

                var roleResult = await _userManager.AddToRoleAsync(newUser, role.Name);
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return ServiceResult<UserDto>.Failure(UserManagementErrors.UpdateFailed("Nie udało się przypisać roli: " + errors));
                }

                await transaction.CommitAsync();

                user.UserId = newUser.Id;
                return ServiceResult<UserDto>.Success(user);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating user {Email}", user.Email);
                return ServiceResult<UserDto>.Failure(CommonErrors.InternalServerError("Błąd zapisu nowego użytkownika."));
            }
        }

        public async Task<ServiceResult> UpdateUser(UserDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return ServiceResult.Failure(AuthErrors.EmailRequired());
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                    return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, model.UserId));

                var role = await _context.Roles.FindAsync(model.RoleId);
                if (role == null)
                    return ServiceResult.Failure(UserManagementErrors.RoleNotFound(model.RoleId));

                if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var existingUserWithNewEmail = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUserWithNewEmail != null && existingUserWithNewEmail.Id != user.Id)
                    {
                        return ServiceResult.Failure(AuthErrors.UserAlreadyExists());
                    }
                    user.Email = model.Email;
                }

                user.UserName = model.UserName;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return ServiceResult.Failure(UserManagementErrors.UpdateFailed(errors));
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                await _userManager.AddToRoleAsync(user, role.Name!);

                await transaction.CommitAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error updating roles/user {model.UserId}");
                return ServiceResult.Failure(CommonErrors.UpdateFailed(ObjectName, model.UserId));
            }
        }

        public async Task<ServiceResult<UserDto>> GetUserById(string userId)
        {
            try
            {
                var userData = await _context.Users
                     .Where(u => u.Id == userId)
                     .Join(_context.UserRoles,
                           u => u.Id,
                           ur => ur.UserId,
                           (u, ur) => new { User = u, UserRole = ur })
                     .Join(_context.Roles,
                           ur => ur.UserRole.RoleId,
                           r => r.Id,
                           (ur, r) => new
                           {
                               User = ur.User,
                               Role = r
                           })
                     .FirstOrDefaultAsync();

                if (userData == null)
                    return ServiceResult<UserDto>.Failure(CommonErrors.NotFound(ObjectName, userId));

                var userDto = new UserDto
                {
                    UserId = userData.User.Id,
                    UserName = userData.User.UserName ?? string.Empty,
                    Email = userData.User.Email ?? string.Empty,
                    RoleId = userData.Role.Id,
                    RoleName = userData.Role.Name ?? string.Empty
                };

                return ServiceResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetUserById error {userId}");
                return ServiceResult<UserDto>.Failure(CommonErrors.FetchFailed(ObjectName));
            }
        }
    }
}