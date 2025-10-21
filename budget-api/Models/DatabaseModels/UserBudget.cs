using Microsoft.AspNetCore.Identity;

namespace budget_api.Models.DatabaseModels
{
    public class UserBudget
    {
        public string UserId { get; set; }
        public int BudgetId { get; set; }


        public IdentityUser User { get; set; }
        public Budget Budget { get; set; }
        public UserRoleInBudget Role { get; set; }
    }

    public enum UserRoleInBudget
    {
        Owner,
        Member
    }
}