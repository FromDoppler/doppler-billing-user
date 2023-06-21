
using Doppler.BillingUser.ExternalServices.Clover.Responses;
using Doppler.BillingUser.ExternalServices.FirstData;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Clover
{
    public interface ICloverService
    {
        Task<bool> IsValidCreditCard(string accountname, CreditCard creditCard, int clientId, bool isFreeUser);
        Task<string> CreateCreditCardPayment(string accountname, decimal chargeTotal, CreditCard creditCard, int clientId, bool isFreeUser, bool isReprocessCall);

        Task<CustomerResponse> CreateCustomerAsync(string email, string name, CreditCard creditCard);

        Task<CustomerResponse> UpdateCustomerAsync(string email, string name, CreditCard creditCard, string cloverCustomerId);

        Task<CustomerResponse> GetCustomerAsync(string email);
    }
}
