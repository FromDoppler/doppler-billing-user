namespace Doppler.BillingUser.Model
{
    public class BillingCreditPaymentInfo
    {
        public string CCNumber { get; set; }
        public int CCExpMonth { get; set; }
        public int CCExpYear { get; set; }
        public string CCVerification { get; set; }
    }
}
