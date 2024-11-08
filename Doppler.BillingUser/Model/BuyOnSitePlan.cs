namespace Doppler.BillingUser.Model
{
    public class BuyOnSitePlan
    {
        public int PlanId { get; set; }
        public int DiscountId { get; set; }
        public decimal? Total { get; set; }
        public string OriginInbound { get; set; }
    }
}
