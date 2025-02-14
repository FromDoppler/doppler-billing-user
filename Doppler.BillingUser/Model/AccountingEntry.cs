using Doppler.BillingUser.Enums;
using System;

namespace Doppler.BillingUser.Model
{
    public class AccountingEntry
    {
        public int IdAccountingEntry { get; set; }
        public int IdInvoice { get; set; }
        public int IdClient { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public PaymentStatusEnum Status { get; set; }
        public SourceTypeEnum Source { get; set; }
        public string AccountingTypeDescription { get; set; }
        public int InvoiceNumber { get; set; }
        public int IdAccountType { get; set; }
        public int IdInvoiceBillingType { get; set; }
        public int? IdBillingSource { get; set; }
        public string AuthorizationNumber { get; set; }
        public string AccountEntryType { get; set; }
        public string CcCNumber { get; set; }
        public int CcExpMonth { get; set; }
        public int CcExpYear { get; set; }
        public string CcHolderName { get; set; }
        public string PaymentEntryType { get; set; }
        public int IdCurrencyType { get; set; }
        public decimal? CurrencyRate { get; set; }
        public double? Taxes { get; set; }
        public string ErrorMessage { get; set; }
    }
}
