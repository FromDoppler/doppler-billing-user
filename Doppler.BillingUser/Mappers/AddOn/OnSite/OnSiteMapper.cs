using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.AddOn.OnSite
{
    public class OnSiteMapper(IOnSitePlanRepository onSitePlanRepository, IBillingRepository billingRepository) : IAddOnMapper
    {
        private readonly IOnSitePlanRepository onSitePlanRepository = onSitePlanRepository;
        private readonly IBillingRepository billingRepository = billingRepository;

        public Task<int> CreateAddOnPlanUserAsync(AddOnPlanUser addOnPlanUser)
        {
            var onSitePlanUser = new OnSitePlanUser
            {
                Activated = addOnPlanUser.Activated,
                ActivationDate = addOnPlanUser.ActivationDate,
                ExperirationDate = addOnPlanUser.ExperirationDate,
                IdBillingCredit = addOnPlanUser.IdBillingCredit,
                IdOnSitePlan = addOnPlanUser.IdAddOnPlan,
                IdUser = addOnPlanUser.IdUser
            };

            return billingRepository.CreateOnSitePlanUserAsync(onSitePlanUser);
        }

        public async Task<AddOnPlan> GetAddOnFreePlanAsync()
        {
            var onSitePlan = await onSitePlanRepository.GetFreeOnSitePlan();
            return new AddOnPlan
            {
                Description = onSitePlan.Description,
                Fee = onSitePlan.Fee,
                FreeDays = onSitePlan.FreeDays,
                PlanId = onSitePlan.IdOnSitePlan,
                Quantity = onSitePlan.PrintQty,
            };
        }

        public AddOnPlanUser MapToPlanUser(int userId, int addOnPlanId, int? billingCreditId)
        {
            DateTime now = DateTime.UtcNow;

            return new AddOnPlanUser
            {
                IdUser = userId,
                Activated = true,
                IdBillingCredit = billingCreditId,
                IdAddOnPlan = addOnPlanId,
                ActivationDate = now,
            };
        }
    }
}
