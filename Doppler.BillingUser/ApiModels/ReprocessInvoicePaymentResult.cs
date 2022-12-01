using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.ApiModels
{
    public class ReprocessInvoicePaymentResult
    {
        public PaymentStatusEnum Result { get; set; }
        public string PaymentError { get; set; }
        public int InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
