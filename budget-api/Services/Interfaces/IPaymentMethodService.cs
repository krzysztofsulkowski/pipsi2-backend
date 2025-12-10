using budget_api.Models.Dto;
using budget_api.Services.Results;

namespace budget_api.Services.Interfaces
{
    public interface IPaymentMethodService
    {
        Task<ServiceResult<List<PaymentMethodDto>>> GetAllPaymentMethodsAsync();
    }
}
