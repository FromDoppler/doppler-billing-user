using System;
using System.Reflection;

namespace Doppler.BillingUser.Model
{
    public class DeclinedInvoiceData
    {
        public DateTime Date { get; set; }
        public int InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
