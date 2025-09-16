namespace Doppler.BillingUser.Reponse
{
    public class ApplyDiscountResponse
    {
        public bool CanApplyDiscount { get; set; }
        public bool HaveActiveDiscount { get; set; }
        public bool UsedDiscountLastPeriod { get; set; }
    }
}
