using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;
using System;

namespace Doppler.BillingUser.Mappers.AddOn.PushNotification
{
    public class PushNotificationMapper(IPushNotificationPlanRepository pushNotificationPlanRepository, IBillingRepository billingRepository) : IAddOnMapper
    {
        private readonly IPushNotificationPlanRepository pushNotificationPlanRepository = pushNotificationPlanRepository;
        private readonly IBillingRepository billingRepository = billingRepository;

        public Task<int> CreateAddOnPlanUserAsync(AddOnPlanUser addOnPlanUser)
        {
            var pushNotificationPlanUser = new PushNotificationPlanUser
            {
                Activated = addOnPlanUser.Activated,
                ActivationDate = addOnPlanUser.ActivationDate,
                ExperirationDate = addOnPlanUser.ExperirationDate,
                IdBillingCredit = addOnPlanUser.IdBillingCredit,
                IdPushNotificationPlan = addOnPlanUser.IdAddOnPlan,
                IdUser = addOnPlanUser.IdUser
            };

            return billingRepository.CreatePushNotificationPlanUserAsync(pushNotificationPlanUser);
        }

        public async Task<AddOnPlan> GetAddOnFreePlanAsync()
        {
            var pushNotificationPlan = await pushNotificationPlanRepository.GetFreePlan();
            return new AddOnPlan
            {
                Description = pushNotificationPlan.Description,
                Fee = pushNotificationPlan.Fee,
                FreeDays = pushNotificationPlan.FreeDays,
                PlanId = pushNotificationPlan.IdPushNotificationPlan,
                Quantity = pushNotificationPlan.Quantity,
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
