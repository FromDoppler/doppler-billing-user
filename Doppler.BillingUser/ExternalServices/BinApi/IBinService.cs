using Doppler.BillingUser.ExternalServices.BinApi.Responses;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.BinApi
{
    public interface IBinService
    {
        Task<IsAllowedCreditCardResponse> IsAllowedCreditCard(string cardNumber);
    }
}
