using System.Collections.Generic;

namespace Doppler.BillingUser.Model
{
    public class ReprocessInvoiceResult
    {
        public bool allInvoicesProcessed { get; set; }
        public bool anyPendingInvoices { get; set; }
    }
}
