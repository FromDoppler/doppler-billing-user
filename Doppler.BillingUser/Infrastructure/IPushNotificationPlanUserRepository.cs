using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IPushNotificationPlanUserRepository
    {
        Task<CurrentPlan> GetCurrentPlan(string accountname);
    }
}
