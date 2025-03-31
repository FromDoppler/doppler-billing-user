using System;

namespace Doppler.BillingUser.Model
{
    public class PushNotificationPlanUser
    {
        public int IdUser { get; set; }
        public int IdPushNotificationPlan { get; set; }
        public bool Activated { get; set; }
        public int? IdBillingCredit { get; set; }
        public DateTime? ActivationDate { get; set; }
        public DateTime? ExperirationDate { get; set; }
    }
}
