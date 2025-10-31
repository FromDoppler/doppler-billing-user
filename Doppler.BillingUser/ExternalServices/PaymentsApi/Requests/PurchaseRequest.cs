using Doppler.BillingUser.ExternalServices.FirstData;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Requests
{
    public class PurchaseRequest
    {
        public string PaymentToken { get; set; } = null!;
        public decimal Amount { get; set; }
        public CreditCard EncryptedCreditCard { get; set; }
        public int CustomerId { get; set; }
        public string CustomerEmail { get; set; }
    }
}
