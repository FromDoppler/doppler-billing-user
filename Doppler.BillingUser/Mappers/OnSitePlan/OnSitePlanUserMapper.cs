using Doppler.BillingUser.Model;
using System;

namespace Doppler.BillingUser.Mappers.OnSitePlan
{
    public class OnSitePlanUserMapper : IOnSitePlanUserMapper
    {
        public OnSitePlanUser MapToOnSitePlanUser(int idUser, int idOnSitePlan, int? idBillingCredit)
        {
            DateTime now = DateTime.UtcNow;

            return new OnSitePlanUser
            {
                IdUser = idUser,
                Activated = true,
                IdBillingCredit = idBillingCredit,
                IdOnSitePlan = idOnSitePlan,
                ActivationDate = now,
            };
        }
    }
}
