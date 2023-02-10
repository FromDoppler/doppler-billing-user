using System;
using System.Drawing;

namespace Doppler.BillingUser.Model
{
    public class BillingCredit
    {
        public int IdBillingCredit { get; set; }
        public int IdUser { get; set; }
        public DateTime? ActivationDate { get; set; }
        public int? TotalCreditsQty { get; set; }
        public int? CreditsQty { get; set; }
        public int IdUserTypePlan { get; set; }
        public DateTime Date { get; set; }
        public decimal PlanFee { get; set; }
        public int DiscountPlanFee { get; set; }
        public int IdResponsabileBilling { get; set; }
        public string CCIdentificationType { get; set; }
        public int? TotalMonthPlan { get; set; }
        public string Cuit { get; set; }
        public int? DiscountPlanFeeAdmin { get; set; }
        public int? DiscountPlanFeePromotion { get; set; }
        public int? IdPromotion { get; set; }
        public int? SubscribersQty { get; set; }
        public int? RemainingCredits { get; set; }
        public DateTime? PaymentDate { get; set; }
        public int? IdDiscountPlan { get; set; }
        public int? CurrentMonthPlan { get; set; }
        public int IdPaymentMethod { get; set; }
        public int IdBillingCreditType { get; set; }
        public int? ExtraCreditsPromotion { get; set; }
        public int IdUserType { get; set; }
        public string CCNumber { get; set; }
        public string CCHolderName { get; set; }
    }
}
