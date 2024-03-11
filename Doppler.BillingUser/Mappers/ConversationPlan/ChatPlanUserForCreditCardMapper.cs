using Doppler.BillingUser.Model;
using System;

namespace Doppler.BillingUser.Mappers.ConversationPlan
{
    public class ChatPlanUserForCreditCardMapper : IChatPlanUserMapper
    {
        public ChatPlanUser MapToChatPlanUser(int idUser, int idChatPlan, int idBillingCredit)
        {
            DateTime now = DateTime.UtcNow;

            return new ChatPlanUser
            {
                IdUser = idUser,
                Activated = true,
                IdBillingCredit = idBillingCredit,
                IdChatPlan = idChatPlan,
                ActivationDate = now,
            };
        }
    }
}
