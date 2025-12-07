using System;

namespace budget_api.Models.DatabaseModels
{
    public enum TransactionType
    {
        Income = 0,
        Expense = 1
    }

    public enum ExpenseStatus
    {
        Instant = 0,
        Recurring = 1,
        Planned = 2
    }

    public enum Frequency
    {
        Weekly = 0,
        BiWeekly = 1,
        Monthly = 2,
        Yearly = 3
    }

    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        Blik = 2,
        Transfer = 3,
        Other = 10
    }

    public class BudgetTransaction
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }


        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public TransactionType Type { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }
        public ExpenseStatus? Status { get; set; }

        public string? ReceiptImageUrl { get; set; }
        public Frequency? Frequency { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
