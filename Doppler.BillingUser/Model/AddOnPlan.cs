namespace Doppler.BillingUser.Model
{
    public class AddOnPlan
    {
        public int PlanId { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal Fee { get; set; }
        public int? FreeDays { get; set; }

        public AddOnPlan() { }

        //Mejorar esto
        public AddOnPlan(OnSitePlan onSitePlan)
        {
            PlanId = onSitePlan.IdOnSitePlan;
            Description = onSitePlan.Description;
            Quantity = onSitePlan.PrintQty;
            Fee = onSitePlan.Fee;
            FreeDays = onSitePlan.FreeDays;
        }
    }
}
