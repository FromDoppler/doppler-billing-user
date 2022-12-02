using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public interface IPaymentGateway
    {
        Task<bool> IsValidCreditCard(CreditCard creditCard, int clientId, bool isFreeUser);
        Task<string> CreateCreditCardPayment(decimal chargeTotal, CreditCard creditCard, int clientId, bool isFreeUser, bool isReprocessCall);
    }
}
