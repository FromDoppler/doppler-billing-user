using System;

namespace Doppler.BillingUser.Model
{
    public class BillingCreditModel
    {
        public int BillingCreditsID { get; set; }
        public DateTime Date { get; set; }
        public int ClientID { get; set; }
        public int? IdUserTypePlan { get; set; }
        public int? IdPaymentMethod { get; set; }
        public bool Approved { get; set; }
        public double? PlanFee { get; set; }
        public double? ExtraEmailFee { get; set; }
        public int? DiscountPlanFeeAdmin { get; set; }
        public int? DiscountPlanFeePromotion { get; set; }
        public int? PromotionDuration { get; set; }
        public double? Taxes { get; set; }
        public bool Payed { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? ActivationDate { get; set; }
        public int? CreditsQty { get; set; }
        public int? SubscribersQty { get; set; }
        public int? ExtraCreditsPromotion { get; set; }
        public int? DiscountPlanFee { get; set; }
        public int? IdDiscountPlan { get; set; }
        public int? MonthPlan { get; set; }
        public int? TotalMonthPlan { get; set; }
        public int? CurrentMonthPlan { get; set; }
        public int IdBillingCreditType { get; set; }
    }
}
