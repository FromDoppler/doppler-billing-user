using Doppler.BillingUser.ExternalServices.Clover.Entities;

namespace Doppler.BillingUser.ExternalServices.Clover.Requests
{
    public class CustomerRequest
    {
        public string CloverCustomerId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public CreditCard CreditCard { get; set; }
    }
}
