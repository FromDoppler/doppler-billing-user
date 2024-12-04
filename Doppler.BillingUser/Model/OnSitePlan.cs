namespace Doppler.BillingUser.Model
{
    public class OnSitePlan
    {
        public int IdOnSitePlan { get; set; }
        public string Description { get; set; }
        public int PrintQty { get; set; }
        public decimal Fee { get; set; }
        public int? FreeDays { get; set; }
    }
}
