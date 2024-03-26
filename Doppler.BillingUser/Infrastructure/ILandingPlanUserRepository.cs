using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface ILandingPlanUserRepository
    {
        Task<int> CreateLandingPlanUserAsync(LandingPlanUser landingPlanUser);
    }
}
