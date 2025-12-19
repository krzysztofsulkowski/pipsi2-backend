using budget_api.Models.DatabaseModels;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ServiceResult<List<Category>>> GetAllCategoriesAsync();
    }
}
