using System.Collections.Generic;

namespace Doppler.BillingUser.Model
{
    public class GetDeclinedInvoicesResult
    {
        public decimal TotalPending { get; set; }
        public List<DeclinedInvoiceData> Invoices { get; set; }
    }
}
