using System;

namespace Doppler.BillingUser.Model
{
    public class ChatPlanUser
    {
        public int IdUser { get; set; }
        public int IdChatPlan { get; set; }
        public bool Activated { get; set; }
        public int IdBillingCredit { get; set; }
        public DateTime? ActivationDate { get; set; }
    }
}
