using budget_api.Models;
using budget_api.Models.DatabaseModels;
using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using budget_api.Services.Errors; 
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace budget_api.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly BudgetApiDbContext _context;
        private readonly ILogger<TransactionService> _logger;
        private readonly IEmailService _emailService;
        private const string IncomeObj = "Income";
        private const string ExpenseObj = "Expense";
        private const string TransactionObj = "Transaction";

        public TransactionService(BudgetApiDbContext context, ILogger<TransactionService> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        private async Task<bool> UserIsMemberAsync(int budgetId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            return await _context.UserBudgets.AnyAsync(ub => ub.BudgetId == budgetId && ub.UserId == userId);
        }

        private async Task<decimal> ComputeBalanceAsync(int budgetId)
        {
            var incomes = await _context.BudgetTransactions
                .Where(t => t.BudgetId == budgetId && t.Type == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var expenses = await _context.BudgetTransactions
                .Where(t => t.BudgetId == budgetId && t.Type == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            return incomes - expenses;
        }

        public async Task<ServiceResult<TransactionDetailsDto>> AddIncomeAsync(int budgetId, CreateIncomeDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult<TransactionDetailsDto>.Failure(TransactionErrors.NoAccess());

                var tx = new BudgetTransaction
                {
                    BudgetId = budgetId,
                    Title = model.Description,
                    Amount = model.Amount,
                    Date = model.Date,
                    Type = TransactionType.Income,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                };

                await _context.BudgetTransactions.AddAsync(tx);
                await _context.SaveChangesAsync();

                return ServiceResult<TransactionDetailsDto>.Success(new TransactionDetailsDto
                {
                    Id = tx.Id,
                    BudgetId = tx.BudgetId,
                    Title = tx.Title,
                    Description = tx.Title,
                    Amount = tx.Amount,
                    Date = tx.Date,
                    Type = tx.Type,
                    CategoryId = tx.CategoryId,
                    CategoryName = tx.Category?.Name,
                    PaymentMethod = tx.PaymentMethod,
                    Status = tx.Status,
                    ReceiptImageUrl = tx.ReceiptImageUrl,
                    Frequency = tx.Frequency,
                    EndDate = tx.EndDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd dodawania przychodu: Budżet {BudgetId}", budgetId);
                return ServiceResult<TransactionDetailsDto>.Failure(CommonErrors.CreateFailed(IncomeObj));
            }
        }

        public async Task<ServiceResult<TransactionDetailsDto>> GetIncomeDetailsAsync(int budgetId, int incomeId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult<TransactionDetailsDto>.Failure(TransactionErrors.NoAccess());

                var tx = await _context.BudgetTransactions
                    .Include(t => t.Category)
                    .FirstOrDefaultAsync(t => t.Id == incomeId && t.BudgetId == budgetId && t.Type == TransactionType.Income);

                if (tx == null)
                    return ServiceResult<TransactionDetailsDto>.Failure(TransactionErrors.IncomeNotFound(incomeId));

                return ServiceResult<TransactionDetailsDto>.Success(new TransactionDetailsDto
                {
                    Id = tx.Id,
                    BudgetId = tx.BudgetId,
                    Title = tx.Title,
                    Description = tx.Title,
                    Amount = tx.Amount,
                    Date = tx.Date,
                    Type = tx.Type,
                    CategoryId = tx.CategoryId,
                    CategoryName = tx.Category?.Name,
                    PaymentMethod = tx.PaymentMethod,
                    Status = tx.Status,
                    ReceiptImageUrl = tx.ReceiptImageUrl,
                    Frequency = tx.Frequency,
                    EndDate = tx.EndDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd pobierania przychodu {IncomeId}", incomeId);
                return ServiceResult<TransactionDetailsDto>.Failure(CommonErrors.FetchFailed(IncomeObj));
            }
        }

        public async Task<ServiceResult> EditIncomeAsync(int budgetId, int incomeId, CreateIncomeDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure(TransactionErrors.NoAccess());

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == incomeId && t.BudgetId == budgetId && t.Type == TransactionType.Income);
                if (tx == null) return ServiceResult.Failure(TransactionErrors.IncomeNotFound(incomeId));

                tx.Title = model.Description;
                tx.Amount = model.Amount;
                tx.Date = model.Date;

                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd edycji przychodu {IncomeId}", incomeId);
                return ServiceResult.Failure(CommonErrors.UpdateFailed(IncomeObj, incomeId));
            }
        }

        public async Task<ServiceResult> DeleteIncomeAsync(int budgetId, int incomeId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure(TransactionErrors.NoAccess());

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == incomeId && t.BudgetId == budgetId && t.Type == TransactionType.Income);
                if (tx == null) return ServiceResult.Failure(TransactionErrors.IncomeNotFound(incomeId));

                _context.BudgetTransactions.Remove(tx);
                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd usuwania przychodu {IncomeId}", incomeId);
                return ServiceResult.Failure(CommonErrors.DeleteFailed(IncomeObj, incomeId));
            }
        }

        public async Task<ServiceResult> AddExpenseAsync(int budgetId, CreateExpenseDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure(TransactionErrors.NoAccess());

                var balance = await ComputeBalanceAsync(budgetId);
                if (model.Amount > balance)
                    return ServiceResult.Failure(TransactionErrors.InsufficientFunds());

                if (model.ExpenseType == ExpenseStatus.Instant)
                {
                    model.Frequency = null;
                    model.EndDate = null;
                }

                var tx = new BudgetTransaction
                {
                    BudgetId = budgetId,
                    Title = model.Description,
                    Amount = model.Amount,
                    Date = model.StartDate ?? DateTime.UtcNow.Date,
                    Type = TransactionType.Expense,
                    CategoryId = model.CategoryId,
                    PaymentMethod = model.PaymentMethod,
                    Status = model.ExpenseType,
                    ReceiptImageUrl = model.ReceiptImageUrl,
                    Frequency = model.Frequency,
                    EndDate = model.EndDate,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                };

                await _context.BudgetTransactions.AddAsync(tx);
                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd dodawania wydatku: Budżet {BudgetId}", budgetId);
                return ServiceResult.Failure(CommonErrors.CreateFailed(ExpenseObj));
            }
        }

        public async Task<ServiceResult<TransactionDetailsDto>> GetExpenseDetailsAsync(int budgetId, int expenseId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult<TransactionDetailsDto>.Failure(TransactionErrors.NoAccess());

                var tx = await _context.BudgetTransactions
                    .Include(t => t.Category)
                    .FirstOrDefaultAsync(t => t.Id == expenseId && t.BudgetId == budgetId && t.Type == TransactionType.Expense);

                if (tx == null) return ServiceResult<TransactionDetailsDto>.Failure(TransactionErrors.ExpenseNotFound(expenseId));

                return ServiceResult<TransactionDetailsDto>.Success(new TransactionDetailsDto
                {
                    Id = tx.Id,
                    BudgetId = tx.BudgetId,
                    Title = tx.Title,
                    Description = tx.Title,
                    Amount = tx.Amount,
                    Date = tx.Date,
                    Type = tx.Type,
                    CategoryId = tx.CategoryId,
                    CategoryName = tx.Category?.Name,
                    PaymentMethod = tx.PaymentMethod,
                    Status = tx.Status,
                    ReceiptImageUrl = tx.ReceiptImageUrl,
                    Frequency = tx.Frequency,
                    EndDate = tx.EndDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd pobierania wydatku {ExpenseId}", expenseId);
                return ServiceResult<TransactionDetailsDto>.Failure(CommonErrors.FetchFailed(ExpenseObj));
            }
        }

        public async Task<ServiceResult> EditExpenseAsync(int budgetId, int expenseId, CreateExpenseDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure(TransactionErrors.NoAccess());

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == expenseId && t.BudgetId == budgetId && t.Type == TransactionType.Expense);
                if (tx == null) return ServiceResult.Failure(TransactionErrors.ExpenseNotFound(expenseId));

                var incomes = await _context.BudgetTransactions.Where(t => t.BudgetId == budgetId && t.Type == TransactionType.Income).SumAsync(t => (decimal?)t.Amount) ?? 0m;
                var otherExpenses = await _context.BudgetTransactions.Where(t => t.BudgetId == budgetId && t.Type == TransactionType.Expense && t.Id != expenseId).SumAsync(t => (decimal?)t.Amount) ?? 0m;
                var available = incomes - otherExpenses;

                if (model.Amount > available)
                    return ServiceResult.Failure(TransactionErrors.BalanceChangeDenied());

                if (model.ExpenseType == ExpenseStatus.Instant)
                {
                    model.Frequency = null;
                    model.EndDate = null;
                }

                tx.Title = model.Description;
                tx.Amount = model.Amount;
                tx.Date = model.StartDate ?? tx.Date;
                tx.CategoryId = model.CategoryId;
                tx.PaymentMethod = model.PaymentMethod;
                tx.Status = model.ExpenseType;
                tx.ReceiptImageUrl = model.ReceiptImageUrl;
                tx.Frequency = model.Frequency;
                tx.EndDate = model.EndDate;

                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd edycji wydatku {ExpenseId}", expenseId);
                return ServiceResult.Failure(CommonErrors.UpdateFailed(ExpenseObj, expenseId));
            }
        }

        public async Task<ServiceResult> DeleteExpenseAsync(int budgetId, int expenseId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure(TransactionErrors.NoAccess());

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == expenseId && t.BudgetId == budgetId && t.Type == TransactionType.Expense);
                if (tx == null) return ServiceResult.Failure(TransactionErrors.ExpenseNotFound(expenseId));

                _context.BudgetTransactions.Remove(tx);
                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd usuwania wydatku {ExpenseId}", expenseId);
                return ServiceResult.Failure(CommonErrors.DeleteFailed(ExpenseObj, expenseId));
            }
        }

        public async Task<ServiceResult<DataTableResponse<TransactionListItemDto>>> SearchTransactionsAsync(int budgetId, Models.Dto.DataTableRequest request, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult<DataTableResponse<TransactionListItemDto>>.Failure(TransactionErrors.NoAccess());

                string[] columnNames = { "Date", "Title", "Amount", "Type", "Category", "Status", "PaymentMethod", "UserName" };
                string sortColumn = (request.OrderColumn >= 0 && request.OrderColumn < columnNames.Length) ? columnNames[request.OrderColumn] : "Date";

                var baseQuery = _context.BudgetTransactions
                    .Where(t => t.BudgetId == budgetId)
                    .Include(t => t.Category)
                    .AsQueryable();

                var totalRecords = await baseQuery.CountAsync();

                if (!string.IsNullOrEmpty(request.SearchValue))
                {
                    var sv = request.SearchValue.ToLower();
                    var categoryIds = await _context.Categories
                        .Where(c => c.Name.ToLower().Contains(sv))
                        .Select(c => c.Id)
                        .ToListAsync();
                    
                    var paymentMethodStrings = new[] { "cash", "card", "blik", "transfer", "other" };
                    var matchingPaymentMethods = new List<PaymentMethod>();
                    if (paymentMethodStrings.Any(pm => pm.Contains(sv)))
                    {
                        if ("cash".Contains(sv)) matchingPaymentMethods.Add(PaymentMethod.Cash);
                        if ("card".Contains(sv)) matchingPaymentMethods.Add(PaymentMethod.Card);
                        if ("blik".Contains(sv)) matchingPaymentMethods.Add(PaymentMethod.Blik);
                        if ("transfer".Contains(sv)) matchingPaymentMethods.Add(PaymentMethod.Transfer);
                        if ("other".Contains(sv)) matchingPaymentMethods.Add(PaymentMethod.Other);
                    }
                    
                    baseQuery = baseQuery.Where(t =>
                        (t.Title != null && t.Title.ToLower().Contains(sv)) ||
                        (t.CategoryId != null && categoryIds.Contains(t.CategoryId.Value)) ||
                        (t.PaymentMethod.HasValue && matchingPaymentMethods.Contains(t.PaymentMethod.Value))
                    );
                }

                var recordsFiltered = await baseQuery.CountAsync();

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    string orderExpr = sortColumn switch
                    {
                        "Date" => "Date",
                        "Title" => "Title",
                        "Amount" => "Amount",
                        "Type" => "Type",
                        "Category" => "Category.Name",
                        "Status" => "Status",
                        "PaymentMethod" => "PaymentMethod",
                        "UserName" => "CreatedByUserId",
                        _ => "Date"
                    };
                    var dir = string.Equals(request.OrderDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                    baseQuery = baseQuery.OrderBy(orderExpr + " " + dir);
                }

                var data = await baseQuery
                    .Skip(request.Start)
                    .Take(request.Length)
                    .Select(t => new TransactionListItemDto
                    {
                        Id = t.Id,
                        Date = t.Date,
                        Title = t.Title,
                        Amount = t.Amount,
                        Type = t.Type,
                        CategoryName = t.Category != null ? t.Category.Name : null,
                        Status = t.Status,
                        PaymentMethod = t.PaymentMethod,
                        UserName = _context.Users
                            .Where(u => u.Id == t.CreatedByUserId)
                            .Select(u => u.UserName)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return ServiceResult<DataTableResponse<TransactionListItemDto>>.Success(new DataTableResponse<TransactionListItemDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = totalRecords,
                    RecordsFiltered = recordsFiltered,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd SearchTransactions dla budżetu {BudgetId}", budgetId);
                return ServiceResult<DataTableResponse<TransactionListItemDto>>.Failure(CommonErrors.FetchFailed(TransactionObj));
            }
        }

        public async Task<ServiceResult<List<TransactionListItemDto>>> GetTransactionsForStatsAsync(int budgetId, int year, int month)
        {
            try
            {
                var query = _context.BudgetTransactions
                    .Include(t => t.Category)
                    .Include(t => t.User)
                    .Where(t => t.BudgetId == budgetId);

                if (month > 0)
                {
                    query = query.Where(t => t.Date.Year == year && t.Date.Month == month);
                }
                else
                {
                    query = query.Where(t => t.Date.Year == year);
                }

                var list = await query.Select(t => new TransactionListItemDto
                {
                    Id = t.Id,
                    Date = t.Date,
                    Title = (string.IsNullOrEmpty(t.Title)) ? (t.Type == TransactionType.Income ? "Przychód" : "Wydatek") : t.Title,
                    Amount = t.Amount,
                    Type = t.Type,
                    CategoryName = t.Category != null ? t.Category.Name : "Inne",
                    Status = t.Status,
                    PaymentMethod = t.PaymentMethod,
                    UserName = t.User != null ? t.User.UserName : "Nieznany"
                }).ToListAsync();

                return ServiceResult<List<TransactionListItemDto>>.Success(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd pobierania statystyk dla budżetu {BudgetId}", budgetId);
                return ServiceResult<List<TransactionListItemDto>>.Failure(CommonErrors.FetchFailed("Statystyki"));
            }
        }

        public async Task<ServiceResult> ProcessRecurringAndPlannedExpensesAsync()
        {
            var today = DateTime.UtcNow.Date;

            try
            {
                var potentialExpenses = await _context.BudgetTransactions
                    .Include(t => t.User)
                    .Include(t => t.Category)
                    .Where(t => t.Type == TransactionType.Expense)
                    .Where(t => t.Status == ExpenseStatus.Recurring || t.Status == ExpenseStatus.Planned)
                    .Where(t => t.EndDate == null || t.EndDate.Value.Date >= today)
                    .Where(t => t.Date.Date <= today)
                    .OrderBy(t => t.BudgetId)
                    .ToListAsync();

                var distinctBudgetIds = potentialExpenses.Select(e => e.BudgetId).Distinct().ToList();
                var budgetNamesMap = await _context.Budgets
                    .Where(b => distinctBudgetIds.Contains(b.Id))
                    .ToDictionaryAsync(k => k.Id, v => v.Name);

                var balanceCache = new Dictionary<int, decimal>();

                foreach (var expense in potentialExpenses)
                {
                    try
                    {
                        if (!balanceCache.ContainsKey(expense.BudgetId))
                            balanceCache[expense.BudgetId] = await ComputeBalanceAsync(expense.BudgetId);

                        var currentBalance = balanceCache[expense.BudgetId];
                        var budgetName = budgetNamesMap.GetValueOrDefault(expense.BudgetId, "Budżet");
                        var requiredAmount = expense.Amount;
                        var expenseCreatorEmail = expense.User?.Email;

                        if (string.IsNullOrEmpty(expenseCreatorEmail)) continue;

                        if (requiredAmount > currentBalance)
                        {
                            await _emailService.SendRecurrentExpenseFailedNotificationAsync(
                                expenseCreatorEmail, budgetName, expense.Title, expense.Amount, "Brak wystarczających środków na koncie w budżecie.");

                            if (expense.Status == ExpenseStatus.Planned)
                            {
                                expense.Date = today.AddDays(1);
                                _context.BudgetTransactions.Update(expense);
                            }
                            await _context.SaveChangesAsync();
                            continue;
                        }

                        BudgetTransaction processedTx = expense;

                        if (expense.Status == ExpenseStatus.Recurring)
                        {
                            processedTx = new BudgetTransaction
                            {
                                BudgetId = expense.BudgetId,
                                Title = expense.Title,
                                Amount = expense.Amount,
                                Date = DateTime.UtcNow,
                                Type = TransactionType.Expense,
                                CategoryId = expense.CategoryId,
                                PaymentMethod = expense.PaymentMethod,
                                Status = ExpenseStatus.Instant,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = expense.CreatedByUserId
                            };
                            await _context.BudgetTransactions.AddAsync(processedTx);
                            expense.Date = GetNextDate(expense);
                            _context.BudgetTransactions.Update(expense);
                        }
                        else if (expense.Status == ExpenseStatus.Planned)
                        {
                            expense.Status = ExpenseStatus.Instant;
                            expense.Date = DateTime.UtcNow;
                            expense.Frequency = null;
                            expense.EndDate = null;
                            _context.BudgetTransactions.Update(expense);
                        }

                        await _context.SaveChangesAsync();
                        balanceCache[expense.BudgetId] -= requiredAmount;

                        await _emailService.SendRecurrentExpenseSuccessNotificationAsync(
                            expenseCreatorEmail, budgetName, processedTx.Title, processedTx.Amount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Błąd Joba: Transakcja {ExpenseId}", expense.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd krytyczny Joba.");
                return ServiceResult.Failure(CommonErrors.InternalServerError("Błąd krytyczny zadania harmonogramu."));
            }

            return ServiceResult.Success();
        }

        private DateTime GetNextDate(BudgetTransaction transaction)
        {
            DateTime lastDate = transaction.Date;
            return transaction.Frequency switch
            {
                Frequency.Weekly => lastDate.AddDays(7),
                Frequency.BiWeekly => lastDate.AddDays(14),
                Frequency.Monthly => lastDate.AddMonths(1),
                Frequency.Yearly => lastDate.AddYears(1),
                _ => lastDate.AddMonths(1)
            };
        }
    }
}
