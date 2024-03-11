using System.Collections.Generic;

namespace Doppler.BillingUser.Model
{
    public class AgreementInformation
    {
        public int PlanId { get; set; }
        public int DiscountId { get; set; }
        public decimal? Total { get; set; }
        public string Promocode { get; set; }
        public string OriginInbound { get; set; }
        public IList<AdditionalService> AdditionalServices { get; set; }
    }
}
