using System.Collections.Generic;

namespace Doppler.BillingUser.ApiModels
{
    public class GetDeclinedInvoicesResult
    {
        public decimal TotalPending { get; set; }
        public List<DeclinedInvoiceData> Invoices { get; set; }
    }
}
