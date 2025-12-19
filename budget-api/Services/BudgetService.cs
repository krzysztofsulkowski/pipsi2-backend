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
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return ServiceResult.Failure("Nazwa budżetu jest wymagana.");
                }

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

         public async Task<ServiceResult<DataTableResponse<BudgetDataTableDto>>> GetUserBudgetsDataTableAsync(string userId, DataTableRequest request)
        {
            try
            {
                string[] columnNames = { "Name", "CreationDate", "Status", "UserRole" };
                string sortColumn = (request.OrderColumn >= 0 && request.OrderColumn < columnNames.Length) ? columnNames[request.OrderColumn] : "CreationDate";
                bool sortDesc = string.Equals(request.OrderDir, "desc", StringComparison.OrdinalIgnoreCase);

                var query = _context.UserBudgets
                    .Where(ub => ub.UserId == userId)
                    .Include(ub => ub.Budget)
                    .AsQueryable();

                var totalRecords = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    string search = request.SearchValue.ToLower();
                    query = query.Where(ub =>
                        (ub.Budget.Name != null && ub.Budget.Name.ToLower().Contains(search)) ||
                        ((ub.Budget.IsArchived ? "zarchiwizowany" : "aktywny").Contains(search)) ||
                        ((ub.Role == UserRoleInBudget.Owner ? "właściciel" : "członek").Contains(search))
                    );
                }

                var recordsFiltered = await query.CountAsync();

                query = sortColumn switch
                {
                    "Name" => sortDesc ? query.OrderByDescending(ub => ub.Budget.Name) : query.OrderBy(ub => ub.Budget.Name),
                    "CreationDate" => sortDesc ? query.OrderByDescending(ub => ub.Budget.CreationDate) : query.OrderBy(ub => ub.Budget.CreationDate),
                    "Status" => sortDesc ? query.OrderByDescending(ub => ub.Budget.IsArchived) : query.OrderBy(ub => ub.Budget.IsArchived),
                    "UserRole" => sortDesc ? query.OrderByDescending(ub => ub.Role) : query.OrderBy(ub => ub.Role),
                    _ => sortDesc ? query.OrderByDescending(ub => ub.Budget.CreationDate) : query.OrderBy(ub => ub.Budget.CreationDate)
                };

                var data = await query
                    .Skip(request.Start)
                    .Take(request.Length)
                    .Select(ub => new BudgetDataTableDto
                    {
                        Name = ub.Budget.Name,
                        CreationDate = ub.Budget.CreationDate,
                        Status = ub.Budget.IsArchived ? "Zarchiwizowany" : "Aktywny",
                        UserRole = ub.Role == UserRoleInBudget.Owner ? "Właściciel" : "Członek"
                    })
                    .ToListAsync();

                var response = new DataTableResponse<BudgetDataTableDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = totalRecords,
                    RecordsFiltered = recordsFiltered,
                    Data = data
                };

                return ServiceResult<DataTableResponse<BudgetDataTableDto>>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy budżetów w formacie DataTable dla użytkownika {UserId}.", userId);
                return ServiceResult<DataTableResponse<BudgetDataTableDto>>.Failure(CommonErrors.DataProcessingError());
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

        public async Task<ServiceResult<List<BudgetMemberDto>>> GetBudgetMembersAsync(int budgetId, string userId)
        {
            try
            {
                var hasAccess = await _context.UserBudgets
                    .AnyAsync(ub => ub.BudgetId == budgetId && ub.UserId == userId);

                if (!hasAccess)
                {
                    return ServiceResult<List<BudgetMemberDto>>.Failure(CommonErrors.NotFound(ObjectName, budgetId));
                }

                var userBudgets = await _context.UserBudgets
                    .Where(ub => ub.BudgetId == budgetId)
                    .Include(ub => ub.User)
                    .Include(ub => ub.Budget)
                    .ToListAsync();

                var memberDtos = userBudgets.Select(ub =>
                {
                    var displayName = ub.User?.UserName;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = ub.User?.Email ?? "Nieznany użytkownik";
                    }

                    return new BudgetMemberDto
                    {
                        UserId = ub.UserId,
                        User = displayName,
                        Date = ub.Budget?.CreationDate ?? DateTime.UtcNow,
                        Role = ub.Role == UserRoleInBudget.Owner ? "Właściciel" : "Członek",
                        Status = "Aktywny"
                    };
                }).ToList();

                var invitations = await _context.BudgetInvitations
                    .Where(i => i.BudgetId == budgetId && i.Status != InvitationStatus.Accepted)
                    .ToListAsync();

                if (invitations.Any())
                {
                    var invitedEmails = invitations
                        .Select(i => i.InvitedUserEmail.ToLowerInvariant())
                        .Distinct()
                        .ToList();

                    var invitedUsers = await _userManager.Users
                        .Where(u => u.Email != null && invitedEmails.Contains(u.Email.ToLower()))
                        .ToDictionaryAsync(u => u.Email!.ToLower(), u => u);

                    foreach (var invitation in invitations)
                    {
                        invitedUsers.TryGetValue(invitation.InvitedUserEmail.ToLowerInvariant(), out var invitedUser);

                        var displayName = invitedUser?.UserName;
                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = invitedUser?.Email ?? invitation.InvitedUserEmail;
                        }

                        memberDtos.Add(new BudgetMemberDto
                        {
                            UserId = invitedUser?.Id,
                            User = displayName,
                            Date = invitation.CreatedAt,
                            Role = "Członek",
                            Status = invitation.Status switch
                            {
                                InvitationStatus.Pending => "Zaproszenie Wysłane",
                                InvitationStatus.Declined => "Zaproszenie Odrzucone",
                                InvitationStatus.Expired => "Zaproszenie Wygasło",
                                _ => "Zaproszenie Wysłane"
                            }
                        });
                    }
                }

                return ServiceResult<List<BudgetMemberDto>>.Success(memberDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy członków budżetu {BudgetId} dla użytkownika {UserId}.", budgetId, userId);
                return ServiceResult<List<BudgetMemberDto>>.Failure(CommonErrors.FetchFailed(ObjectName, budgetId.ToString()));
            }
        }
    }
}