using System;

namespace Doppler.BillingUser.Model
{
    public class OnSitePlanUser
    {
        public int IdUser { get; set; }
        public int IdOnSitePlan { get; set; }
        public bool Activated { get; set; }
        public int IdBillingCredit { get; set; }
        public DateTime? ActivationDate { get; set; }
    }
}
