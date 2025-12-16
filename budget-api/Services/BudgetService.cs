using budget_api.Models;
using budget_api.Models.DatabaseModels;
using budget_api.Models.Dto;
using budget_api.Models.ViewModel;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using budget_api.Services.Errors;

namespace budget_api.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly BudgetApiDbContext _context;
        private readonly ILogger<BudgetService> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private const string ObjectName = "Budget";

        public BudgetService(BudgetApiDbContext context, ILogger<BudgetService> logger, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<ServiceResult> CreateBudgetAsync(BudgetViewModel model, string userId)
        {
            try
            {
                var newBudget = new Budget
                {
                    Name = model.Name
                };

                var userBudgetLink = new UserBudget
                {
                    UserId = userId,
                    Budget = newBudget,
                    Role = UserRoleInBudget.Owner
                };

                await _context.Budgets.AddAsync(newBudget);
                await _context.UserBudgets.AddAsync(userBudgetLink);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Użytkownik {UserId} pomyślnie utworzył nowy budżet o ID {BudgetId}.", userId, newBudget.Id);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas tworzenia budżetu dla użytkownika {UserId}.", userId);
                return ServiceResult.Failure(CommonErrors.CreateFailed(ObjectName));
            }
        }

        public async Task<ServiceResult<BudgetViewModel>> GetBudgetByIdAsync(int budgetId, string userId)
        {
            try
            {
                var budget = await _context.Budgets
                    .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserBudgets.Any(ub => ub.UserId == userId));

                if (budget == null)
                {
                    _logger.LogWarning("Użytkownik {UserId} próbował uzyskać dostęp do nieistniejącego lub niedozwolonego budżetu {BudgetId}.", userId, budgetId);
                    return ServiceResult<BudgetViewModel>.Failure(CommonErrors.NotFound(ObjectName, budgetId));
                }
                var budgetDto = new BudgetViewModel
                {
                    Id = budget.Id,
                    Name = budget.Name
                };

                return ServiceResult<BudgetViewModel>.Success(budgetDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas pobierania budżetu {BudgetId} dla użytkownika {UserId}.", budgetId, userId);
                return ServiceResult<BudgetViewModel>.Failure(CommonErrors.FetchFailed(ObjectName));
            }
        }

        public async Task<ServiceResult> AcceptInvitationAsync(Guid token)
        {

            var invitation = await _context.BudgetInvitations
                .FirstOrDefaultAsync(i => i.Token == token && i.Status == InvitationStatus.Pending);

            if (invitation == null)
            {
                return ServiceResult.Failure(InvitationError.InvalidOrUsedError());
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _context.SaveChangesAsync();
                return ServiceResult.Failure(InvitationError.ExpiredError());
            }

            var user = await _userManager.FindByEmailAsync(invitation.InvitedUserEmail);

            if (user == null)
            {
                return ServiceResult.Failure(InvitationError.UserRegistrationRequiredError());
            }

            var isAlreadyMember = await _context.UserBudgets
                .AnyAsync(ub => ub.BudgetId == invitation.BudgetId && ub.UserId == user.Id);

            if (isAlreadyMember)
            {
                invitation.Status = InvitationStatus.Accepted;
                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }

            var userBudgetLink = new UserBudget
            {
                BudgetId = invitation.BudgetId,
                UserId = user.Id,
                Role = UserRoleInBudget.Member
            };
            await _context.UserBudgets.AddAsync(userBudgetLink);

            invitation.Status = InvitationStatus.Accepted;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Użytkownik {UserId} pomyślnie zaakceptował zaproszenie i dołączył do budżetu {BudgetId}", user.Id, invitation.BudgetId);

            return ServiceResult.Success();
        }


        public async Task<ServiceResult<InvitationResultData>> CreateInvitationAsync(int budgetId, string recipientEmail, string inviterUserId)
        {
            try
            {
                var hasAccess = await _context.UserBudgets
                    .AnyAsync(ub => ub.BudgetId == budgetId && ub.UserId == inviterUserId);

                if (!hasAccess)
                {
                    _logger.LogWarning("Użytkownik {InviterId} próbował wysłać zaproszenie do budżetu {BudgetId}, do którego nie ma dostępu.", inviterUserId, budgetId);
                    return ServiceResult<InvitationResultData>.Failure(InvitationError.PermissionDeniedError());
                }

                var invitedUser = await _userManager.FindByEmailAsync(recipientEmail);
                if (invitedUser != null)
                {
                    var isAlreadyMember = await _context.UserBudgets
                        .AnyAsync(ub => ub.BudgetId == budgetId && ub.UserId == invitedUser.Id);
                    if (isAlreadyMember)
                    {
                        return ServiceResult<InvitationResultData>.Failure(InvitationError.AlreadyMemberError());
                    }
                }

                var invitation = new BudgetInvitation
                {
                    BudgetId = budgetId,
                    InvitedUserEmail = recipientEmail.ToLowerInvariant(),
                    Token = Guid.NewGuid(),
                    Status = InvitationStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await _context.BudgetInvitations.AddAsync(invitation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Utworzono nowe zaproszenie (ID: {InvitationId}) do budżetu {BudgetId} dla {Email}.", invitation.Id, budgetId, recipientEmail);

                var resultData = new InvitationResultData
                {
                    Invitation = invitation,
                    UserExistsInSystem = (invitedUser != null)
                };

                return ServiceResult<InvitationResultData>.Success(resultData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia zaproszenia do budżetu {BudgetId} dla {Email}.", budgetId, recipientEmail);
                return ServiceResult<InvitationResultData>.Failure(CommonErrors.InternalServerError());
            }
        }


        public async Task<ServiceResult> ArchiveBudgetAsync(int budgetId, string userId)
        {
            try
            {
                var userBudget = await _context.UserBudgets
                    .FirstOrDefaultAsync(ub => ub.BudgetId == budgetId && ub.UserId == userId);

                if (userBudget == null)
                    return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, budgetId));

                if (userBudget.Role != UserRoleInBudget.Owner)
                    return ServiceResult.Failure(BudgetErrors.OnlyOwnerCanArchive());

                var budget = await _context.Budgets.FindAsync(budgetId);
                if (budget == null) return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, budgetId));

                if (budget.IsArchived)
                    return ServiceResult.Failure(BudgetErrors.AlreadyArchived());

                budget.IsArchived = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Budżet {BudgetId} został zarchiwizowany przez użytkownika {UserId}.", budgetId, userId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ArchiveBudgetAsync failed");
                return ServiceResult.Failure(CommonErrors.UpdateFailed(ObjectName, budgetId));
            }
        }


        public async Task<ServiceResult> UnarchiveBudgetAsync(int budgetId, string userId)
        {
            try
            {
                var userBudget = await _context.UserBudgets
                    .FirstOrDefaultAsync(ub => ub.BudgetId == budgetId && ub.UserId == userId);

                if (userBudget == null)
                    return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, budgetId));

                if (userBudget.Role != UserRoleInBudget.Owner)
                    return ServiceResult.Failure(BudgetErrors.OnlyOwnerCanRestore());

                var budget = await _context.Budgets.FindAsync(budgetId);
                if (budget == null) return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, budgetId));

                if (!budget.IsArchived)
                    return ServiceResult.Failure(BudgetErrors.NotArchived());

                budget.IsArchived = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Budżet {BudgetId} został przywrócony z archiwum przez użytkownika {UserId}.", budgetId, userId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przywracania budżetu {BudgetId} przez użytkownika {UserId}.", budgetId, userId);
                return ServiceResult.Failure(CommonErrors.UpdateFailed(ObjectName, budgetId));
            }
        }

        public async Task<ServiceResult<List<BudgetSummaryViewModel>>> GetUserBudgetsAsync(string userId)
        {
            try
            {
                var query = await _context.UserBudgets
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Budget)
                .Select(ub => new BudgetSummaryViewModel
                {
                    Id = ub.BudgetId,
                    Name = ub.Budget.Name,
                    CreationDate = ub.Budget.CreationDate,
                    Status = ub.Budget.IsArchived ? "Zarchiwizowany" : "Aktywny",
                    Role = ub.Role == UserRoleInBudget.Owner ? "Właściciel" : "Członek"
                })
                .OrderByDescending(b => b.CreationDate)
                .ToListAsync();

                return ServiceResult<List<BudgetSummaryViewModel>>.Success(query);            
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania budżetów dla użytkownika {UserId}", userId);
                return ServiceResult<List<BudgetSummaryViewModel>>.Failure(CommonErrors.FetchFailed(ObjectName));
            }
        }

        public async Task<ServiceResult> EditBudgetAsync(int budgetId, EditBudgetViewModel model, string userId)
        {
            try
            {
                var userBudget = await _context.UserBudgets
                    .FirstOrDefaultAsync(ub => ub.BudgetId == budgetId && ub.UserId == userId);

                if (userBudget == null)
                    return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, budgetId));

                if (userBudget.Role != UserRoleInBudget.Owner)
                    return ServiceResult.Failure(BudgetErrors.OnlyOwnerCanEdit());

                var budget = await _context.Budgets.FindAsync(budgetId);
                if (budget == null) return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, budgetId));

                if (!string.IsNullOrWhiteSpace(model.Name))
                    budget.Name = model.Name.Trim();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Użytkownik {UserId} zmodyfikował budżet {BudgetId}.", userId, budgetId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas edycji budżetu {BudgetId} przez użytkownika {UserId}.", budgetId, userId);
                return ServiceResult.Failure(CommonErrors.UpdateFailed(ObjectName, budgetId));
            }
        }

        public async Task<ServiceResult> RemoveMemberAsync(int budgetId, string targetUserId, string currentUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(targetUserId))
                    return ServiceResult.Failure(CommonErrors.BadRequest("ID użytkownika jest wymagane."));

                var currentUserBudget = await _context.UserBudgets
                    .FirstOrDefaultAsync(ub => ub.BudgetId == budgetId && ub.UserId == currentUserId);
                if (currentUserBudget == null)
                    return ServiceResult.Failure(CommonErrors.NotFound(ObjectName, budgetId));

                var targetUserBudget = await _context.UserBudgets
                    .FirstOrDefaultAsync(ub => ub.BudgetId == budgetId && ub.UserId == targetUserId);
                if (targetUserBudget == null)
                    return ServiceResult.Failure(BudgetErrors.MemberNotFound());

                if (currentUserBudget.Role == UserRoleInBudget.Owner)
                {
                    if (currentUserId == targetUserId)
                        return ServiceResult.Failure(BudgetErrors.OwnerCannotLeave());

                    if (targetUserBudget.Role == UserRoleInBudget.Owner)
                        return ServiceResult.Failure(BudgetErrors.CannotRemoveOwner());

                    _context.UserBudgets.Remove(targetUserBudget);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Właściciel {UserId} usunął użytkownika {TargetUserId} z budżetu {BudgetId}.", currentUserId, targetUserId, budgetId);
                    return ServiceResult.Success();
                }
                else
                {
                    if (currentUserId != targetUserId)
                        return ServiceResult.Failure(CommonErrors.Forbidden("Możesz usunąć tylko samego siebie."));

                    _context.UserBudgets.Remove(currentUserBudget);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Użytkownik {UserId} opuścił budżet {BudgetId}.", currentUserId, budgetId);
                    return ServiceResult.Success();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania członka {TargetUserId} z budżetu {BudgetId} przez {CurrentUserId}.", targetUserId, budgetId, currentUserId);
                return ServiceResult.Failure(CommonErrors.DeleteFailed("UserBudget"));
            }
        }
    }
}