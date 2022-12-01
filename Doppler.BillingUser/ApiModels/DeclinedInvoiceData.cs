using System;
using System.Reflection;

namespace Doppler.BillingUser.ApiModels
{
    public class DeclinedInvoiceData
    {
        public DateTime Date { get; set; }
        public int InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Error { get; set; }
    }
}
