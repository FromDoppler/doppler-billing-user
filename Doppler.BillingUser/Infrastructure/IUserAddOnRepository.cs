using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserAddOnRepository
    {
        Task<UserAddOn> GetByUserIdAndAddOnType(int userId, int addOnType);

        Task SaveCurrentBillingCreditByUserIdAndAddOnTypeAsync(int userId, int addOnType, int billingCreditId);
    }
}
