using Doppler.BillingUser.Enums;
using System;

namespace Doppler.BillingUser.Model
{
    public class UserBillingInformation
    {
        public int IdUser { get; set; }
        public PaymentMethodEnum PaymentMethod { get; set; }
        public int IdBillingCountry { get; set; }
        public string Email { get; set; }
        public bool ResponsableIVA { get; set; }
        public string PaymentWay { get; set; }
        public string PaymentType { get; set; }
        public string CFDIUse { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public int? IdCurrentBillingCredit { get; set; }
        public DateTime? UTCFirstPayment { get; set; }
        public string Cuit { get; set; }
        public string OriginInbound { get; set; }
        public bool? UpgradePending { get; set; }
        public DateTime? UTCUpgrade { get; set; }
        public int MaxSubscribers { get; set; }
        public bool IsCancelated { get; set; }
        public int TaxRegime { get; set; }
        public string TaxRegimeDescription { get; set; }
        public string Cbu { get; set; }
        public string WorldPayToken { get; set; }
    }
}
