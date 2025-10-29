using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi
{
    public interface IPaymentsService
    {
        Task<string> GeneratePaymentToken(string WorldPayLowValueToken, string CardNumber);
    }
}
