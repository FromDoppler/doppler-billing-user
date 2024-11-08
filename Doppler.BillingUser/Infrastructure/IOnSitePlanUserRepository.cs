using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IOnSitePlanUserRepository
    {
        Task<CurrentPlan> GetCurrentPlan(string accountname);
    }
}
