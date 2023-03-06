namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapCreditNoteDto
    {
        public int InvoiceId { get; set; }
        public double Amount { get; set; }
        public int ClientId { get; set; }
        public int BillingSystemId { get; set; }
        public int Type { get; set; }
        public string Reason { get; set; }
        public string CardErrorCode { get; set; }
        public string CardErrorDetail { get; set; }
        public bool TransactionApproved { get; set; }
        public string TransferReference { get; set; }
        public int CreditNoteId { get; set; }
    }
}
