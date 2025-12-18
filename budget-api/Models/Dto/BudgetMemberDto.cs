using System;

namespace budget_api.Models.Dto
{
    public class BudgetMemberDto
    {
        public string? UserId { get; set; }
        public string User { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

