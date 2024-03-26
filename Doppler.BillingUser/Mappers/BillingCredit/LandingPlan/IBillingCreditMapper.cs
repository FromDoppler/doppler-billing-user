using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit.LandingPlan
{
    public interface IBillingCreditMapper
    {
        Task<BillingCreditAgreement> MapToBillingCreditAgreement(decimal total, UserBillingInformation user, Model.BillingCredit currentBillingCredit, CreditCardPayment payment, BillingCreditTypeEnum billingCreditType);
    }
}
