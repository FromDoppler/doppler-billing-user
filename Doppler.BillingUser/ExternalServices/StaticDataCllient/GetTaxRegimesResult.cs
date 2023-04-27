using Doppler.BillingUser.Model;
using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.StaticDataCllient
{
    public class GetTaxRegimesResult
    {
        public bool isSuccesful { get; set; }
        public List<TaxRegime> TaxRegimes { get; set; }
    }
}
