using System.ComponentModel;

namespace Doppler.BillingUser.Enums
{
    public enum PaymentStatusEnum
    {
        [Description("Approved")]
        Approved,
        [Description("Pending")]
        Pending,
        [Description("Declined")]
        DeclinedPaymentTransaction
    }
}
