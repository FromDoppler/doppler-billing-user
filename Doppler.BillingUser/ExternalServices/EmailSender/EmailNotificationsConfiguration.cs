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
    }
}
