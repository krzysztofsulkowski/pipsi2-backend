using budget_api.Models;
using budget_api.Models.DatabaseModels;
using budget_api.Models.Dto;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace budget_api.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly BudgetApiDbContext _context;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(BudgetApiDbContext context, ILogger<TransactionService> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<ServiceResult> AddIncomeAsync(int budgetId, CreateIncomeDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure("Brak uprawnień do tego budżetu.");

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
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania przychodu do budżetu {BudgetId}", budgetId);
                return ServiceResult.Failure("Błąd podczas dodawania przychodu.");
            }
        }

        public async Task<ServiceResult<TransactionDetailsDto>> GetIncomeDetailsAsync(int budgetId, int incomeId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult<TransactionDetailsDto>.Failure("Brak uprawnień.");

                var tx = await _context.BudgetTransactions
                    .Include(t => t.Category)
                    .FirstOrDefaultAsync(t => t.Id == incomeId && t.BudgetId == budgetId && t.Type == TransactionType.Income);

                if (tx == null) return ServiceResult<TransactionDetailsDto>.Failure("Nie znaleziono przychodu.");

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
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów przychodu {IncomeId} dla budżetu {BudgetId}", incomeId, budgetId);
                return ServiceResult<TransactionDetailsDto>.Failure("Błąd podczas pobierania szczegółów przychodu.");
            }
        }

        public async Task<ServiceResult> EditIncomeAsync(int budgetId, int incomeId, CreateIncomeDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure("Brak uprawnień.");

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == incomeId && t.BudgetId == budgetId && t.Type == TransactionType.Income);
                if (tx == null) return ServiceResult.Failure("Nie znaleziono przychodu.");

                tx.Title = model.Description;
                tx.Amount = model.Amount;
                tx.Date = model.Date;

                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas edycji przychodu {IncomeId}", incomeId);
                return ServiceResult.Failure("Błąd podczas edycji przychodu.");
            }
        }

        public async Task<ServiceResult> DeleteIncomeAsync(int budgetId, int incomeId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure("Brak uprawnień.");

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == incomeId && t.BudgetId == budgetId && t.Type == TransactionType.Income);
                if (tx == null) return ServiceResult.Failure("Nie znaleziono przychodu.");

                _context.BudgetTransactions.Remove(tx);
                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania przychodu {IncomeId}", incomeId);
                return ServiceResult.Failure("Błąd podczas usuwania przychodu.");
            }
        }

        public async Task<ServiceResult> AddExpenseAsync(int budgetId, CreateExpenseDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure("Brak uprawnień do tego budżetu.");

                var balance = await ComputeBalanceAsync(budgetId);
                if (model.Amount > balance)
                    return ServiceResult.Failure("Brak wystarczających środków w budżecie. Operacja odrzucona.");

                var tx = new BudgetTransaction
                {
                    BudgetId = budgetId,
                    Title = model.Description,
                    Amount = model.Amount,
                    Date = model.StartDate ?? DateTime.UtcNow,
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
                _logger.LogError(ex, "Błąd podczas dodawania wydatku do budżetu {BudgetId}", budgetId);
                return ServiceResult.Failure("Błąd podczas dodawania wydatku.");
            }
        }

        public async Task<ServiceResult<TransactionDetailsDto>> GetExpenseDetailsAsync(int budgetId, int expenseId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult<TransactionDetailsDto>.Failure("Brak uprawnień.");

                var tx = await _context.BudgetTransactions
                    .Include(t => t.Category)
                    .FirstOrDefaultAsync(t => t.Id == expenseId && t.BudgetId == budgetId && t.Type == TransactionType.Expense);

                if (tx == null) return ServiceResult<TransactionDetailsDto>.Failure("Nie znaleziono wydatku.");

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
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów wydatku {ExpenseId}", expenseId);
                return ServiceResult<TransactionDetailsDto>.Failure("Błąd podczas pobierania szczegółów wydatku.");
            }
        }

        public async Task<ServiceResult> EditExpenseAsync(int budgetId, int expenseId, CreateExpenseDto model, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure("Brak uprawnień.");

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == expenseId && t.BudgetId == budgetId && t.Type == TransactionType.Expense);
                if (tx == null) return ServiceResult.Failure("Nie znaleziono wydatku.");

                // compute available = incomes - other expenses (excluding this)
                var incomes = await _context.BudgetTransactions.Where(t => t.BudgetId == budgetId && t.Type == TransactionType.Income).SumAsync(t => (decimal?)t.Amount) ?? 0m;
                var otherExpenses = await _context.BudgetTransactions.Where(t => t.BudgetId == budgetId && t.Type == TransactionType.Expense && t.Id != expenseId).SumAsync(t => (decimal?)t.Amount) ?? 0m;
                var available = incomes - otherExpenses;

                if (model.Amount > available)
                    return ServiceResult.Failure("Zmiana wartości wydatku spowoduje ujemne saldo budżetu. Operacja odrzucona.");

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
                _logger.LogError(ex, "Błąd podczas edycji wydatku {ExpenseId}", expenseId);
                return ServiceResult.Failure("Błąd podczas edycji wydatku.");
            }
        }

        public async Task<ServiceResult> DeleteExpenseAsync(int budgetId, int expenseId, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult.Failure("Brak uprawnień.");

                var tx = await _context.BudgetTransactions.FirstOrDefaultAsync(t => t.Id == expenseId && t.BudgetId == budgetId && t.Type == TransactionType.Expense);
                if (tx == null) return ServiceResult.Failure("Nie znaleziono wydatku.");

                _context.BudgetTransactions.Remove(tx);
                await _context.SaveChangesAsync();
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania wydatku {ExpenseId}", expenseId);
                return ServiceResult.Failure("Błąd podczas usuwania wydatku.");
            }
        }

        public async Task<ServiceResult<DataTableResponse<TransactionListItemDto>>> SearchTransactionsAsync(int budgetId, Models.Dto.DataTableRequest request, string userId)
        {
            try
            {
                if (!await UserIsMemberAsync(budgetId, userId))
                    return ServiceResult<DataTableResponse<TransactionListItemDto>>.Failure("Brak uprawnień.");

                // columns for ordering - must match front-end DataTable column order
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
                    baseQuery = baseQuery.Where(t =>
                        (t.Title != null && t.Title.ToLower().Contains(sv)) ||
                        (t.Category != null && t.Category.Name.ToLower().Contains(sv)) ||
                        (t.PaymentMethod != null && t.PaymentMethod.ToString().ToLower().Contains(sv))
                    );
                }

                var recordsFiltered = await baseQuery.CountAsync();

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    // map column names to actual properties for dynamic order
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
                    var dir = string.Equals(request.OrderDir, "desc", StringComparison.OrdinalIgnoreCase)
                        ? "desc"
                        : "asc";

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

                var response = new DataTableResponse<TransactionListItemDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = totalRecords,
                    RecordsFiltered = recordsFiltered,
                    Data = data
                };

                return ServiceResult<DataTableResponse<TransactionListItemDto>>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wyszukiwania transakcji dla budżetu {BudgetId}", budgetId);
                return ServiceResult<DataTableResponse<TransactionListItemDto>>.Failure("Błąd podczas wyszukiwania transakcji.");
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
                    Title = (t.Title == null || t.Title == string.Empty)
                             ? (t.Type == TransactionType.Income ? "Przychód" : "Wydatek")
                             : t.Title,
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
                _logger.LogError(ex, "Błąd podczas pobierania statystyk dla budżetu {BudgetId}", budgetId);
                return ServiceResult<List<TransactionListItemDto>>.Failure("Nie udało się pobrać danych do statystyk.");
            }
        }
    }
}
