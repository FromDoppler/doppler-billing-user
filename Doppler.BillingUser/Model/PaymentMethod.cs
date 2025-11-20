using Microsoft.AspNetCore.Http;

namespace Doppler.BillingUser.Model
{
    public class PaymentMethod
    {
        public string CCHolderFullName { get; set; }
        public string CCNumber { get; set; }
        public string CCVerification { get; set; }
        public string CCExpMonth { get; set; }
        public string CCExpYear { get; set; }
        public string CCType { get; set; }
        public string PaymentMethodName { get; set; }
        public string RenewalMonth { get; set; }
        public string RazonSocial { get; set; }
        public string IdConsumerType { get; set; }
        public string IdentificationType { get; set; }
        public string IdentificationNumber { get; set; }
        public int IdSelectedPlan { get; set; }
        public bool ResponsableIVA { get; set; }
        public int IdCCType { get; set; }
        public string UseCFDI { get; set; }
        public string PaymentType { get; set; }
        public string PaymentWay { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public int TaxRegime { get; set; }
        public IFormFile TaxCertificate { get; set; }
        public string TaxCertificateUrl { get; set; }
        public string Cbu { get; set; }
        public string WorldPayLowValueToken { get; set; }
        public string LastFourDigitsCCNumber { get; set; }
        public string FirstSixDigitsCCNumber { get; set; }
    }
}
