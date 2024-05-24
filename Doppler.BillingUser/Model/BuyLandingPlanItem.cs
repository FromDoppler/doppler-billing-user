using Newtonsoft.Json;

namespace Doppler.BillingUser.Model
{
    public class BuyLandingPlanItem
    {
        public int LandingPlanId { get; set; }

        [JsonProperty("LandingQty")]
        public int PackQty { get; set; }
        public decimal Fee { get; set; }
    }
}
