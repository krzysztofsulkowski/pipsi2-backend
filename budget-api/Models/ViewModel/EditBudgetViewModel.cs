using System.ComponentModel.DataAnnotations;

namespace budget_api.Models.ViewModel
{
    public class EditBudgetViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}