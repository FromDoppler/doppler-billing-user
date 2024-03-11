namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapAdditionalServiceDto
    {
        public int? ConversationQty { get; set; }
        public double Charge { get; set; }
        public double? DiscountedAmount { get; set; }
        public int Type { get; set; }
    }
}
