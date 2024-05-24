using Doppler.BillingUser.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface ILandingPlanRepository
    {
        Task<IList<LandingPlan>> GetAll();
    }
}
