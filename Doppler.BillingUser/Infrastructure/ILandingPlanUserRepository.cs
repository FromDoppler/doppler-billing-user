using Doppler.BillingUser.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface ILandingPlanUserRepository
    {
        Task<int> CreateLandingPlanUserAsync(LandingPlanUser landingPlanUser);
        Task<IList<LandingPlanUser>> GetLandingPlansByUserIdAndBillingCreditIdAsync(int userId, int billingCreditId);
    }
}
