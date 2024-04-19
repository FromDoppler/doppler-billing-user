using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserAddOnRepository
    {
        Task SaveCurrentBillingCreditByUserIdAndAddOnTypeAsync(int userId, int addOnType, int billingCreditId);
    }
}
