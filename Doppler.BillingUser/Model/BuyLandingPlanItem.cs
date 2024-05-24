using System.Text.Json.Serialization;

namespace Doppler.BillingUser.Model
{
    public class BuyLandingPlanItem
    {
        public int LandingPlanId { get; set; }

        [JsonPropertyName("landingQty")]
        public int PackQty { get; set; }
        public decimal Fee { get; set; }
    }
}
