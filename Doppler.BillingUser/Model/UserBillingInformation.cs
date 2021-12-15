using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class UserBillingInformation
    {
        public int IdUser { get; set; }
        public PaymentMethodEnum PaymentMethod { get; set; }
        public int IdCountry { get; set; }
        public string Email { get; set; }
        public bool ResponsableIVA { get; set; }
        public string PaymentWay { get; set; }
        public string PaymentType { get; set; }
        public string CFDIUse { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public int IdCurrentBillingCredit { get; set; }
        public string UTCFirstPayment { get; set; }
        public string Cuit { get; set; }
    }
}