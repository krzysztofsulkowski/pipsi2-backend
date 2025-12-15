using budget_api.Models.DatabaseModels;
using budget_api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace budget_api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/categories")]
    [Produces("application/json")] 
    public class CategoryController : BudgetApiBaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Pobiera listê wszystkich dostêpnych kategorii transakcji.
        /// </summary>
        /// <remarks>
        /// Endpoint zwraca kategorie (np. Jedzenie, Mieszkanie, Transport), które s³u¿¹ do grupowania przychodów i wydatków.
        /// Dane te s¹ zwykle wykorzystywane w listach rozwijanych (dropdown) w formularzach dodawania transakcji.
        /// </remarks>
        /// <returns>Lista kategorii.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] 
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return HandleServiceResult(result);
        }
    }
}