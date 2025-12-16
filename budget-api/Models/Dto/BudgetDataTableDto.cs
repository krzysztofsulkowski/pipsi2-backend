using System;

namespace budget_api.Models.Dto
{
    public class BudgetDataTableDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
    }
}