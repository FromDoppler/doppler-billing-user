using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IBillingRepository
    {
        Task<BillingInformation> GetBillingInformation(string accountName);

        Task UpdateBillingInformation(string accountName, BillingInformation billingInformation);

        Task<PaymentMethod> GetCurrentPaymentMethod(string username);

        Task<bool> UpdateCurrentPaymentMethod(User user, PaymentMethod paymentMethod);

        Task<EmailRecipients> GetInvoiceRecipients(string accountName);

        Task UpdateInvoiceRecipients(string accountName, string[] emailRecipients, int planId);

        Task<CurrentPlan> GetCurrentPlan(string accountName);

        Task<int> CreateBillingCreditAsync(BillingCreditAgreement buyCreditAgreement);

        Task<int> CreateMovementCreditAsync(int idBillingCredit, int partialBalance, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, int? currentMonthlyAddedEmailsWithBilling = null);

        Task<BillingCredit> GetBillingCredit(int billingCreditId);

        Task UpdateUserSubscriberLimitsAsync(int idUser);

        Task<int> ActivateStandBySubscribers(int idUser);

        Task<PlanDiscountInformation> GetPlanDiscountInformation(int discountId);

        Task SetEmptyPaymentMethod(int idUser);

        Task<int> CreateAccountingEntriesAsync(AccountingEntry invoiceEntry, AccountingEntry paymentEntry);

        Task<PaymentMethod> GetPaymentMethodByUserName(string username);

        Task<AccountingEntry> GetInvoice(int idClient, string authorizationNumber);

        Task UpdateInvoiceStatus(int id, PaymentStatusEnum status, string statusDetail);

        Task<int> CreatePaymentEntryAsync(int invoiceId, AccountingEntry paymentEntry);

        Task<int> CreateMovementBalanceAdjustmentAsync(int userId, int creditsQty, UserTypeEnum currentUserType, UserTypeEnum newUserType);
    }
}
