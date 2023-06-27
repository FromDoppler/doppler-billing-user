using Doppler.BillingUser.ExternalServices.Clover.Entities;

namespace Doppler.BillingUser.ExternalServices.Clover.Requests
{
    public class PaymentRequest
    {
        public decimal ChargeTotal { get; set; }
        public CreditCard CreditCard { get; set; }
        public string ClientId { get; set; }
    }
}
