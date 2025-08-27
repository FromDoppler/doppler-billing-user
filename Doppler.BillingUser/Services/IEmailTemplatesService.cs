using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public interface IEmailTemplatesService
    {
        Task<bool> SendNotificationForSuscribersPlan(string accountname, User userInformation, UserTypePlanInformation newPlan);
        Task<bool> SendActivatedStandByEmail(string language, string fistName, int standByAmount, string sendTo);
        Task SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation, bool isUpgradePending, bool needSendToBilling);
        Task SendNotificationForCredits(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode, bool isUpgradePending, bool needSendToBilling);
        Task SendNotificationForPaymentFailedTransaction(int userId, string errorCode, string errorMessage, string transactionCTR, string bankMessage, PaymentMethodEnum paymentMethod, bool isFreeUser, string cardHolderName, string lastFourDigits);
        Task SendNotificationForMercadoPagoPaymentApproved(int userId, string accountname);
        Task SendNotificationForMercadoPagoPaymentInProcess(int userId, string accountname, string errorCode, string errorMessage, bool isFreeUser);
        Task SendNotificationForUpdatePlan(string accountname, User userInformation, UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation, PlanAmountDetails amountDetails);
        Task SendReprocessStatusNotification(string accountname, int userId, decimal amount, string reprocessStatus, decimal pendingAmount, string paymentMethod);
        Task SendContactInformationForTransferNotification(int userId, string username, string userlastname, string contactemail, string contactphonenumber);
        Task SendNotificationForRejectedMercadoPagoPayment(string accountname, User user, bool isUpgradePending, string paymentStatusDetails);
        Task SendNotificationForChangeIndividualToMontlyOrSubscribers(string accountname, User userInformation, UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation, PlanAmountDetails amountDetails);
        Task SendNotificationForUpgradeLandingPlan(string accountname, User userInformation, UserBillingInformation userBillingInformation, IList<LandingPlan> availableLandingPlans, IList<LandingPlanUser> newLandingPlans, BillingCredit landingBillingCredit, bool isUpgradePending);
        Task SendNotificationForUpdateLandingPlan(string accountname, User userInformation, UserBillingInformation userBillingInformation, IList<LandingPlan> availableLandingPlans, IList<LandingPlanUser> currentLandingPlans, IList<LandingPlanUser> newLandingPlans, BillingCredit landingBillingCredit, PlanAmountDetails amountDetails, bool isUpgradePending);
        Task SendNotificationForUpgradeConversationPlan(string accountname, User userInformation, ChatPlan newPlan, UserBillingInformation user, PlanDiscountInformation planDiscountInformation, bool isUpgradePending, bool needSendToBilling);
        Task SendNotificationForUpdateConversationPlan(string accountname, User userInformation, ChatPlan newPlan, UserBillingInformation user, PlanDiscountInformation planDiscountInformation, PlanAmountDetails amountDetails, CurrentPlan currentPlan, bool isUpgradePending);
        Task SendNotificationForUpgradeAddOnPlan(string accountname, User userInformation, AddOnPlan newPlan, UserBillingInformation user, PlanDiscountInformation planDiscountInformation, bool isUpgradePending, bool needSendToBilling, AddOnType addOnType);
        Task SendNotificationForUpdateAddOnPlan(string accountname, User userInformation, AddOnPlan newPlan, UserBillingInformation user, PlanDiscountInformation planDiscountInformation, PlanAmountDetails amountDetails, CurrentPlan currentPlan, bool isUpgradePending, AddOnType addOnType);
        Task SendNotificationForRequestAdditionalServices(string accountname, User user, BillingCredit currentBillingCredit, PlanDiscountInformation planDiscountInformation, AdditionalServicesRequestModel additionalServicesRequestModel);
        Task SendNotificationForCancelAddOnPlan(string accountname, User userInformation, AddOnType addOnType);
        Task SendNotificationForCancelAccount(string accountname, User userInformation);
        Task SendNotificationForAccountCancellationRequest(string accountname, decimal? planFee, int? userTypeId, int? creditsQty, int? subscribersQty, string contactName, string accountCancellationReason, string contactPhone, string contactSchedule);
        Task SendNotificationForScheduledCancellationRequest(string accountname, decimal? planFee, int? userTypeId, int? creditsQty, int? subscribersQty, string contactName, string accountCancellationReason, string contactPhone, string contactSchedule);
        Task SendNotificationForConsultingOffer(string accountname, decimal? planFee, int? userTypeId, int? creditsQty, int? subscribersQty, string contactName, string accountCancellationReason, string contactPhone, string contactSchedule);
    }
}
