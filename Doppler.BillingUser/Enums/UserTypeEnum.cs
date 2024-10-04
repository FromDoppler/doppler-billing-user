using System.ComponentModel;

namespace Doppler.BillingUser.Enums
{
    public enum UserTypeEnum
    {
        [Description("CM-Monthly")]
        CM_MONTHLY = 0,
        [Description("Free")]
        FREE = 1,
        [Description("Monthly")]
        MONTHLY = 2,
        [Description("Individual")]
        INDIVIDUAL = 3,
        [Description("Subscribers")]
        SUBSCRIBERS = 4
    }
}
