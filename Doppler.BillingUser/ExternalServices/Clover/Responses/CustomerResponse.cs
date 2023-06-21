using Doppler.BillingUser.ExternalServices.Clover.Entities;

namespace Doppler.BillingUser.ExternalServices.Clover.Responses
{
    public class CustomerResponse
    {
        public string Id { get; set; }
        public CustomerCards Cards { get; set; }
    }
}
