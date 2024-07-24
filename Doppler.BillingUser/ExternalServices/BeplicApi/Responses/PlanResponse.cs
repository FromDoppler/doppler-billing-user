namespace Doppler.BillingUser.ExternalServices.BeplicApi.Responses
{
    public class PlanResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PlanContractDate { get; set; } = string.Empty;
        public bool Publish { get; set; }
    }
}
