using budget_api.Models.DatabaseModels;

namespace budget_api.Models.Dto
{
    public class PaymentMethodDto
    {
        public PaymentMethod Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

