namespace Doppler.BillingUser.Request
{
    public class SetScheduledCancellationRequest
    {
        public bool HasScheduledCancellation { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string ContactSchedule { get; set; }
        public string CancellationReason { get; set; }
    }
}
