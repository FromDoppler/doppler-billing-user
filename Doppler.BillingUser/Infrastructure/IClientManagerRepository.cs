using Doppler.BillingUser.ExternalServices.FirstData;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IClientManagerRepository
    {
        Task<CreditCard> GetEncryptedCreditCard(int idClientManager);
    }
}
