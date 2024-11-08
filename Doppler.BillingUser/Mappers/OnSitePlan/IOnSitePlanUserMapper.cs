using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Mappers.OnSitePlan
{
    public interface IOnSitePlanUserMapper
    {
        OnSitePlanUser MapToOnSitePlanUser(int idUser, int idChatPlan, int idBillingCredit);
    }
}
