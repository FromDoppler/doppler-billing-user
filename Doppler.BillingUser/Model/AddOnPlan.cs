namespace Doppler.BillingUser.Model
{
    public class AddOnPlan
    {
        public int PlanId { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal Fee { get; set; }
        public int? FreeDays { get; set; }
        }
    }
}
