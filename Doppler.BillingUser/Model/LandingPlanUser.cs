using System;

namespace Doppler.BillingUser.Model
{
    public class LandingPlanUser
    {
        public int IdLandingPlanUser { get; set; }
        public int IdUser { get; set; }
        public int IdLandingPlan { get; set; }
        public int IdBillingCredit { get; set; }
        public int PackQty { get; set; }
        public decimal Fee { get; set; }
        public DateTime Created { get; set; }
    }
}
