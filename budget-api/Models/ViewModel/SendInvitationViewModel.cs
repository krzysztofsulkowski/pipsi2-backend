using System.ComponentModel.DataAnnotations;

namespace budget_api.Models.ViewModel
{
    public class SendInvitationViewModel
    {
        public string RecipientEmail { get; set; }
        public string BudgetName { get; set; }
        public int BudgetId { get; set; }
    }
}