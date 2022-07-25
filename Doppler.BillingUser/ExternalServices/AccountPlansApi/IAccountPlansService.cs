using System.Threading.Tasks;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public interface IAccountPlansService
    {
        public Task<bool> IsValidTotal(string accountname, AgreementInformation agreementInformation);
        Task<Promotion> GetValidPromotionByCode(string promocode, int planId);
        Task<PlanAmountDetails> GetCalculateUpgrade(string accountName, AgreementInformation agreementInformation);
    }
}
