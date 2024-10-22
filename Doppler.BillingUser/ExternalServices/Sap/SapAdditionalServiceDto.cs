using Doppler.BillingUser.Enums;
using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapAdditionalServiceDto
    {
        public int? ConversationQty { get; set; }
        public double Charge { get; set; }
        public int? Discount { get; set; }
        public double? DiscountedAmount { get; set; }
        public bool IsUpSelling { get; set; }
        public IList<SapPackDto> Packs { get; set; }
        public AdditionalServiceTypeEnum Type { get; set; }
        public bool IsCustom { get; set; }
        public int UserId { get; set; }
    }
}
