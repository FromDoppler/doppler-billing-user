using Doppler.BillingUser.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Doppler.BillingUser.ApiModels
{
    public class GetDeclinedInvoicesResult
    {
        public decimal TotalPending
        {
            get {
                return Invoices.Where(x => x.Status != PaymentStatusApiEnum.Approved).Sum(x => x.Amount);
            }
        }
        public List<DeclinedInvoiceData> Invoices { get; set; }
    }
}
