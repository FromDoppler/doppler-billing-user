namespace Doppler.BillingUser.Model
{
    public class ReprocessInvoiceResult
    {
        public string Message { get; }
        public bool allInvoicesProcessed { get; }

        private ReprocessInvoiceResult(string message, bool allInvoicesProcessed)
        {
            Message = message;
            this.allInvoicesProcessed = allInvoicesProcessed;
        }

        public static ReprocessInvoiceResult Success()
        {
            return new ReprocessInvoiceResult("Successful", true);
        }

        public static ReprocessInvoiceResult Failed(string e)
        {
            return new ReprocessInvoiceResult(e, false);
        }
    }
}
