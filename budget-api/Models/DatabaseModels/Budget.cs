namespace budget_api.Models.DatabaseModels
{
    public class Budget
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public bool IsArchived { get; set; } = false;

        public ICollection<UserBudget> UserBudgets { get; set; } = new List<UserBudget>();
    }
}