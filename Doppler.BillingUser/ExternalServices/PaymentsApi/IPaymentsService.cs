using Doppler.BillingUser.ExternalServices.FirstData;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi
{
    public interface IPaymentsService
    {
        Task<string> GeneratePaymentToken(string WorldPayLowValueToken);
        Task<string> Purchase(string paymentToken, decimal amount, string accountname, CreditCard creditCard, int clientId, bool isFreeUser, string lastFourDigitsCCNumber);
    }
}
