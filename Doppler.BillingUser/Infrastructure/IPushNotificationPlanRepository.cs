using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IPushNotificationPlanRepository
    {
        Task<PushNotificationPlan> GetById(int onSitePlanid);
        Task<PushNotificationPlan> GetFreePlan();
    }
}
