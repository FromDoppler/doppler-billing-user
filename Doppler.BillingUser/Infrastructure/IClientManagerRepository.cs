using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IClientManagerRepository
    {
        Task<CreditCard> GetEncryptedCreditCard(int idClientManager);
        Task<UserBillingInformation> GetUserBillingInformation(int idClientManager);
        Task<User> GetUserInformation(string accountName);
    }
}
