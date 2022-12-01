using Doppler.BillingUser.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BillingUser.ApiModels
{
    public class GetInvoicesResult
    {
        public decimal TotalPending
        {
            get
            {
                return Invoices.Sum(x => x.PendingAmount);
            }
        }
        public List<InvoiceData> Invoices { get; set; }
    }
}
