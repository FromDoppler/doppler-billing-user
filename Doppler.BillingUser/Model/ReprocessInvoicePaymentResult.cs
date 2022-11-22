using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class ReprocessInvoicePaymentResult
    {
        public ReprocessInvoicePaymentResultEnum Result { get; set; }
        public string PaymentError { get; set; }
        public int InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
