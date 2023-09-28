using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class BillingCreditPaymentInfo
    {
        public string CCNumber { get; set; }
        public int? CCExpMonth { get; set; }
        public int? CCExpYear { get; set; }
        public string CCVerification { get; set; }
        public string CCHolderFullName { get; set; }
        public string CCType { get; set; }
        public string PaymentMethodName { get; set; }
        public string IdConsumerType { get; set; }
        public string IdentificationNumber { get; set; }
        public string Cuit { get; set; }
        public string Cbu { get; set; }
        public ResponsabileBillingEnum ResponsabileBilling { get; set; }
    }
}
