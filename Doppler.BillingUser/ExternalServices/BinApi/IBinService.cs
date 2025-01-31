using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.BinApi
{
    public interface IBinService
    {
        Task<bool> IsAllowedCreditCard(string cardNumber);
    }
}
