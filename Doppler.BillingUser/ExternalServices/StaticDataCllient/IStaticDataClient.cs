using Doppler.BillingUser.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.StaticDataCllient
{
    public interface IStaticDataClient
    {
        public Task<GetTaxRegimesResult> GetTaxRegimesAsync();
    }
}
