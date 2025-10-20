using System.ComponentModel.DataAnnotations;

namespace budget_api.Models.DatabaseModels
{
    public class BudgetInvitation
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public string InvitedUserEmail { get; set; }
        public Guid Token { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }


        public Budget Budget { get; set; }
    }

    public enum InvitationStatus
    {
        Pending,
        Accepted,
        Declined,
        Expired
    }
}
