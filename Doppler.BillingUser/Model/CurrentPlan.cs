namespace Doppler.BillingUser.Model
{
    public class CurrentPlan
    {
        public int IdPlan { get; set; }
        public int PlanSubscription { get; set; }
        public string PlanType { get; set; }
        public int RemainingCredits { get; set; }
        public int? EmailQty { get; set; }
        public int? SubscribersQty { get; set; }
        public int? ConversationQty { get; set; }
        public string Description { get; set; }
        public decimal Fee { get; set; }
        public int PaymentMethod { get; set; }
        public int? PrintQty { get; set; }
        public int? Quantity { get; set; }
    }
}
