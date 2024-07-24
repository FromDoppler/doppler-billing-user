namespace Doppler.BillingUser.ExternalServices.BeplicApi.Responses
{
    public class PlanAssignResponse
    {
        public bool Success { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string ActiveDate { get; set; }
        public bool? Active { get; set; }
        public int? TrialPeriod { get; set; }
        public string Error { get; set; }
        public string ErrorStatus { get; set; }
    }
}
