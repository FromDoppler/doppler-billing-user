namespace Doppler.BillingUser.Model
{
    public class PlanAmountDetails
    {
        public decimal DiscountPaymentAlreadyPaid { get; set; }
        public DiscountPrepayment DiscountPrepayment { get; set; }
        public decimal Total { get; set; }
        public decimal CurrentMonthTotal { get; set; }
        public DiscountPromocode DiscountPromocode { get; set; }
        public DiscountPlanFeeAdmin DiscountPlanFeeAdmin { get; set; }
        public decimal NextMonthTotal { get; set; }
        public decimal PositiveBalance { get; set; }
    }
}
