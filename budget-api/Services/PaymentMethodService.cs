using budget_api.Models.DatabaseModels;
using budget_api.Models.Dto;
using budget_api.Services.Errors;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;

namespace budget_api.Services
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly ILogger<PaymentMethodService> _logger;

        public PaymentMethodService(ILogger<PaymentMethodService> logger)
        {
            _logger = logger;
        }

        public async Task<ServiceResult<List<PaymentMethodDto>>> GetAllPaymentMethodsAsync()
        {
            try
            {
                var paymentMethods = Enum.GetValues(typeof(PaymentMethod))
                    .Cast<PaymentMethod>()
                    .Select(pm => new PaymentMethodDto
                    {
                        Value = pm,
                        Name = GetPaymentMethodName(pm)
                    })
                    .OrderBy(pm => pm.Value)
                    .ToList();

                return ServiceResult<List<PaymentMethodDto>>.Success(paymentMethods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas pobierania metod płatności.");
                return ServiceResult<List<PaymentMethodDto>>.Failure(CommonErrors.FetchFailed("PaymentMethods"));
            }
        }

        private static string GetPaymentMethodName(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Gotówka",
                PaymentMethod.Card => "Karta",
                PaymentMethod.Blik => "BLIK",
                PaymentMethod.Transfer => "Przelew",
                PaymentMethod.Other => "Inne",
                _ => method.ToString()
            };
        }
    }
}
