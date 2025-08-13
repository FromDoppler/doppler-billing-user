namespace Doppler.BillingUser.Model
{
    public class AccountCancellationReason
    {
        public int AccountCancellationReasonId { get; set; }
        public bool SendEmailToUser { get; set; }
        public bool Active { get; set; }
    }
}
