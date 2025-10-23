using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit
{
    public interface IBillingCreditMapper
    {
        Task<BillingCreditAgreement> MapToBillingCreditAgreement(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion, CreditCardPayment payment, Model.BillingCredit currentBillingCredit, BillingCreditTypeEnum billingCreditType, Promotion currentPromotion);
        //Task<BillingCreditAgreement> MapToBillingCreditAgreement(UserBillingInformation user, Model.BillingCredit currentBillingCredit);
    }
}
