namespace Doppler.BillingUser.Model
{
    public class FailedToReprocessInvoice
    {
        public int InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Error { get; set; }
    }
}
