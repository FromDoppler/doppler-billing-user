namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Exceptions
{
    public class PaymentError
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TransactionCTR { get; set; }
        public string BankMessage { get; set; }
    }
}
