using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.AddOn
{
    public interface IAddOnMapper
    {
        Task<AddOnPlan> GetAddOnFreePlanAsync();
        AddOnPlanUser MapToPlanUser(int userId, int addOnPlanId, int? billingCreditId);
        Task<int> CreateAddOnPlanUserAsync(AddOnPlanUser addOnPlanUser);
    }
}
