using System.Collections.Generic;

namespace Doppler.BillingUser.Model
{
    public class ReprocessInvoiceResult
    {
        public bool allInvoicesProcessed { get; set; }
        public List<FailedToReprocessInvoice> FailedInvoices { get; set; }
    }
}
