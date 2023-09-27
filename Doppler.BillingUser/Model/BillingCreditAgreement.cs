using System;

namespace Doppler.BillingUser.Model
{
    public class BillingCreditAgreement
    {
        public int IdUser { get; set; }
        public BillingCreditModel BillingCredit { get; set; }
        public int IdPaymentMethod { get; set; }
        public string Code { get; set; }
        public int? IdConsumerType { get; set; }
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string Rfc { get; set; }
        public bool ResponsableIVA { get; set; }
        public string CCNumber { get; set; }
        public short? CCExpMonth { get; set; }
        public short? CCExpYear { get; set; }
        public string CCHolderFullName { get; set; }
        public int? IdCCType { get; set; }
        public string CCVerification { get; set; }
        public string CCIdentificationType { get; set; }
        public string CCIdentificationNumber { get; set; }
        public string ExclusiveMessage { get; set; }
        public int IdCountry { get; set; }
        public string CFDIUse { get; set; }
        public string PaymentWay { get; set; }
        public string PaymentType { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public int IdResponsabileBilling { get; set; }
        public int? IdPromotion { get; set; }
        public int? PromotionDuration { get; set; }
        public string Cbu { get; set; }
    }
}
