using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Model
{
    public class AdditionalService
    {
        public int? PlanId { get; set; }
        public decimal? Charge { get; set; }
        public AdditionalServiceTypeEnum Type { get; set; }
    }
}
