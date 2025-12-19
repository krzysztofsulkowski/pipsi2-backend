using budget_api.Models;
using budget_api.Models.DatabaseModels;
using budget_api.Services.Errors;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace budget_api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly BudgetApiDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(BudgetApiDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<List<Category>>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .OrderBy(c => c.Id)
                    .ToListAsync();

                return ServiceResult<List<Category>>.Success(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas pobierania kategorii.");
                return ServiceResult<List<Category>>.Failure(CommonErrors.FetchFailed("Categories"));
            }
        }
    }
}
