using System.Collections.Generic;
using System.Threading.Tasks;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public interface IAccountPlansService
    {
        public Task<bool> IsValidTotal(string accountname, AgreementInformation agreementInformation);
        Task<Promotion> GetValidPromotionByCode(string promocode, int planId);
        Task<PlanAmountDetails> GetCalculateUpgrade(string accountName, AgreementInformation agreementInformation);
        Task<PlanAmountDetails> GetCalculateLandingUpgrade(string accountName, IEnumerable<int> landingIds, IEnumerable<int> landingPacks);
        Task<PlanAmountDetails> GetCalculateAmountToUpgrade(string accountName, int planType, int planId, int? discountId, string promocode);
    }
}
