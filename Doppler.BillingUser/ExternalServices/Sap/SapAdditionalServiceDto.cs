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
        public string UserEmail { get; set; }
        public int? Quantity { get; set; }

        public double PlanFee { get; set; }
        public int ExtraPeriodMonth { get; set; }
        public int ExtraPeriodYear { get; set; }
        public int? ExtraQty { get; set; }
        public double ExtraFee { get; set; }
        public double ExtraFeePerUnit { get; set; }
    }
}
