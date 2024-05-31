using System.Collections.Generic;

namespace Doppler.BillingUser.Model
{
    public class BuyLandingPlans
    {
        public decimal? Total { get; set; }
        public IList<BuyLandingPlanItem> LandingPlans { get; set; }
    }
}
