using budget_api.Models.DatabaseModels;

namespace budget_api.Models.Dto
{
    public class InvitationResultData
    {
        public BudgetInvitation Invitation { get; set; } = new BudgetInvitation();
        public bool UserExistsInSystem { get; set; }
    }
}