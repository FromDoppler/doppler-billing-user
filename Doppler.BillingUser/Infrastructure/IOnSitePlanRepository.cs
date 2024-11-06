using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IOnSitePlanRepository
    {
        Task<OnSitePlan> GetById(int onSitePlanid);
    }
}
