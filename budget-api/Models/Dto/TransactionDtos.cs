using budget_api.Models.DatabaseModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace budget_api.Models.Dto
{
    public class TransactionListItemDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; } 
        public TransactionType Type { get; set; }
        public string? CategoryName { get; set; }
        public ExpenseStatus? Status { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? UserName { get; set; }
    }

    public class CreateIncomeDto
    {
        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }

    public class CreateExpenseDto : IValidatableObject
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public ExpenseStatus ExpenseType { get; set; }

        public string? ReceiptImageUrl { get; set; }
        public Frequency? Frequency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? PlannedDate { get; set; }
        public DateTime? EndDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExpenseType == ExpenseStatus.Recurring)
            {
                if (Frequency == null)
                {
                    yield return new ValidationResult(
                        "Częstotliwość jest wymagana dla wydatku cyklicznego.",
                        new[] { nameof(Frequency) });
                }

                if (StartDate == null)
                {
                    yield return new ValidationResult(
                        "Data rozpoczęcia jest wymagana dla wydatku cyklicznego.",
                        new[] { nameof(StartDate) });
                }
            }

            if (ExpenseType == ExpenseStatus.Planned)
            {
                if (PlannedDate == null)
                {
                    yield return new ValidationResult(
                        "Data realizacji jest wymagana dla wydatku planowanego.",
                        new[] { nameof(PlannedDate) });
                }
            }
        }
    }

    public class TransactionDetailsDto
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public ExpenseStatus? Status { get; set; }
        public string? ReceiptImageUrl { get; set; }
        public Frequency? Frequency { get; set; }
        public DateTime? EndDate { get; set; }
    }
}