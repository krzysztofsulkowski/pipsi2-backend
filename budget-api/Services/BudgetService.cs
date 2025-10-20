using budget_api.Models;
using budget_api.Models.DatabaseModels;
using budget_api.Models.ViewModel;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace budget_api.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly BudgetApiDbContext _context;
        private readonly ILogger<BudgetService> _logger;
        private readonly UserManager<IdentityUser> _userManager;

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
                return ServiceResult.Failure("Wystąpił nieoczekiwany błąd podczas tworzenia budżetu.");
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
                    return ServiceResult<BudgetViewModel>.Failure("Nie znaleziono budżetu lub brak uprawnień.");
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
                return ServiceResult<BudgetViewModel>.Failure("Wystąpił błąd podczas pobierania danych budżetu.");
            }
        }

        public async Task<ServiceResult> AcceptInvitationAsync(Guid token)
        {
            var invitation = await _context.BudgetInvitations
                .FirstOrDefaultAsync(i => i.Token == token && i.Status == InvitationStatus.Pending);

            if (invitation == null)
            {
                return ServiceResult.Failure("Zaproszenie jest nieprawidłowe lub zostało już wykorzystane.");
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _context.SaveChangesAsync();
                return ServiceResult.Failure("Twoje zaproszenie wygasło.");
            }

            var user = await _userManager.FindByEmailAsync(invitation.InvitedUserEmail);

            if (user == null)
            {
                return ServiceResult.Failure($"Aby zaakceptować zaproszenie, najpierw załóż konto z adresem e-mail: {invitation.InvitedUserEmail}");
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


        public async Task<ServiceResult<BudgetInvitation>> CreateInvitationAsync(int budgetId, string recipientEmail, string inviterUserId)
        {
            try
            {
                var hasAccess = await _context.UserBudgets
                    .AnyAsync(ub => ub.BudgetId == budgetId && ub.UserId == inviterUserId);

                if (!hasAccess)
                {
                    _logger.LogWarning("Użytkownik {InviterId} próbował wysłać zaproszenie do budżetu {BudgetId}, do którego nie ma dostępu.", inviterUserId, budgetId);
                    return ServiceResult<BudgetInvitation>.Failure("Brak uprawnień do zapraszania do tego budżetu.");
                }

                var invitedUser = await _userManager.FindByEmailAsync(recipientEmail);
                if (invitedUser != null)
                {
                    var isAlreadyMember = await _context.UserBudgets
                        .AnyAsync(ub => ub.BudgetId == budgetId && ub.UserId == invitedUser.Id);
                    if (isAlreadyMember)
                    {
                        return ServiceResult<BudgetInvitation>.Failure("Ten użytkownik jest już członkiem tego budżetu.");
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

                return ServiceResult<BudgetInvitation>.Success(invitation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia zaproszenia do budżetu {BudgetId} dla {Email}.", budgetId, recipientEmail);
                return ServiceResult<BudgetInvitation>.Failure("Wystąpił nieoczekiwany błąd serwera.");
            }
        }
    }
}