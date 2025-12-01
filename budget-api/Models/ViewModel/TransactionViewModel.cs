using System;

namespace budget_api.Models.ViewModel
{
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEditTransactionViewModel
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
