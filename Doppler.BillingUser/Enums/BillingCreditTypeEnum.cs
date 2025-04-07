namespace Doppler.BillingUser.Enums
{
    public enum BillingCreditTypeEnum
    {
        UpgradeRequest = 1,
        Credit_Request = 2,
        Credit_Buyed_CC = 3,
        Individual_to_Monthly = 7,
        Individual_to_Subscribers = 8,
        Upgrade_Between_Monthlies = 13,
        Upgrade_Between_Subscribers = 14,
        Canceled = 17,
        Landing_Request = 23,
        Landing_Buyed_CC = 24,
        Landing_Canceled = 25,
        Downgrade_Between_Landings = 26,
        Conversation_Buyed_CC = 28,
        Conversation_Request = 29,
        Conversation_Canceled = 30,
        Downgrade_Between_Conversation = 31,
        OnSite_Buyed_CC = 34,
        OnSite_Request = 35,
        OnSite_Canceled = 36,
        Downgrade_Between_OnSite = 37,

        PushNotification_Buyed_CC = 40,
        PushNotification_Request = 41,
        PushNotification_Canceled = 42,
        Downgrade_Between_PushNotification = 43
    }
}
