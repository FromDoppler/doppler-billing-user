using System.Threading.Tasks;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IPromotionRepository
    {
        Task IncrementUsedTimes(Promotion promocode);
        Task<Promotion> GetById(int promocodeId);
        Task<TimesApplyedPromocode> GetHowManyTimesApplyedPromocode(string code, string accountName, int planType);
        Task DecrementUsedTimesAsync(Promotion promocode);
        Task<Promotion> GetPromotionByCode(string code, int userType, int planId);
        Task<Promotion> GetAddOnPromotionByCodeAndAddOnType(string code, int addOnTypeId);
    }
}
