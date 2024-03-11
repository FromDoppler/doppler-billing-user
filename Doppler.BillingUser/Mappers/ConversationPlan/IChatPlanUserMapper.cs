using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Mappers.ConversationPlan
{
    public interface IChatPlanUserMapper
    {
        ChatPlanUser MapToChatPlanUser(int idUser, int idChatPlan, int idBillingCredit);
    }
}
