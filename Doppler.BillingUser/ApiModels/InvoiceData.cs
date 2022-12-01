using System;
using System.Reflection;

namespace Doppler.BillingUser.ApiModels
{
    public class InvoiceData
    {
        public DateTime Date { get; set; }
        public int InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Error { get; set; }
        public PaymentStatusApiEnum Status { get; set; }
        public decimal PendingAmount
        {
            get
            {
                return (Status == PaymentStatusApiEnum.Approved) ? 0.0M : Amount;
            }
        }
    }
}
