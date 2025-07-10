using System.Collections.Generic;

namespace Doppler.BillingUser.ExternalServices.EmailSender
{
    public class EmailNotificationsConfiguration
    {
        public string AdminEmail { get; set; }
        public string CommercialEmail { get; set; }
        public string BillingEmail { get; set; }
        public string InfoDopplerAppsEmail { get; set; }
        public Dictionary<string, string> CreditsApprovedTemplateId { get; set; }
        public Dictionary<string, string> UpgradeAccountTemplateId { get; set; }
        public Dictionary<string, string> SubscribersPlanPromotionTemplateId { get; set; }
        public string CreditsApprovedAdminTemplateId { get; set; }
        public string UpgradeAccountTemplateAdminTemplateId { get; set; }
        public string UrlEmailImagesBase { get; set; }
        public Dictionary<string, string> ActivatedStandByNotificationTemplateId { get; set; }
        public Dictionary<string, string> CheckAndTransferPurchaseNotification { get; set; }
        public Dictionary<string, string> UpgradeRequestTemplateId { get; set; }
        public string UpgradeRequestAdminTemplateId { get; set; }
        public string CreditsPendingAdminTemplateId { get; set; }
        public string FailedCreditCardFreeUserPurchaseNotificationAdminTemplateId { get; set; }
        public string FailedCreditCardPurchaseNotificationAdminTemplateId { get; set; }
        public string MercadoPagoPaymentApprovedAdminTemplateId { get; set; }
        public string FailedMercadoPagoFreeUserPurchaseNotificationAdminTemplateId { get; set; }
        public string FailedMercadoPagoPurchaseNotificationAdminTemplateId { get; set; }
        public string MercadoPagoFreeUserPaymentInProcessAdminTemplateId { get; set; }
        public string MercadoPagoPaymentInProcessAdminTemplateId { get; set; }
        public Dictionary<string, string> UpdatePlanTemplateId { get; set; }
        public string UpdatePlanAdminTemplateId { get; set; }
        public string ReprocessStatusAdminTemplateId { get; set; }
        public string ContactInformationForTransferAdminTemplateId { get; set; }
        public Dictionary<string, string> DecliendPaymentMercadoPagoUpgradeTemplateId { get; set; }
        public string DecliendPaymentMercadoPagoUpgradeAdminTemplateId { get; set; }
        public Dictionary<string, string> DecliendPaymentMercadoPagoUpsellingTemplateId { get; set; }
        public string DecliendPaymentMercadoPagoUpsellingAdminTemplateId { get; set; }
        public string UpdatePlanCreditsToMontlyOrContactsAdminTemplateId { get; set; }
        public string UpgradeLandingAdminTemplateId { get; set; }
        public string UpdateLandingAdminTemplateId { get; set; }
        public string UpgradeConversationPlanAdminTemplateId { get; set; }
        public string UpgradeConversationPlanRequestAdminTemplateId { get; set; }
        public string UpdateConversationPlanAdminTemplateId { get; set; }
        public string UpgradeAddOnPlanAdminTemplateId { get; set; }
        public string UpgradeAddOnPlanRequestAdminTemplateId { get; set; }
        public string UpdateAddOnPlanAdminTemplateId { get; set; }
        public Dictionary<string, string> SendAdditionalServiceRequestTemplateId { get; set; }
        public string SendAdditionalServiceRequestAdminTemplateId { get; set; }
    }
}
