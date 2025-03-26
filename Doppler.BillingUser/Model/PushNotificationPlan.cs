namespace Doppler.BillingUser.Model
{
    public class PushNotificationPlan
    {
        public int IdPushNotificationPlan { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal Fee { get; set; }
        public int? FreeDays { get; set; }
    }
}
