using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi
{
    public interface IPaymentsService
    {
        Task<string> GeneratePaymentToken(string WorldPayLowValueToken, string CardNumber);
        Task<string> Purchase(string paymentToken, decimal amount);
    }
}
