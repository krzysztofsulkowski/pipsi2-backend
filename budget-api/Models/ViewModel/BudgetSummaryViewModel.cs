namespace budget_api.Models.ViewModel
{
    public class BudgetSummaryViewModel
    {
         public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}