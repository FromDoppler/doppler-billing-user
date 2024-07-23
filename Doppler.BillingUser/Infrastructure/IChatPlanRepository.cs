using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IChatPlanRepository
    {
        Task<ChatPlan> GetById(int chatPlanId);
    }
}
