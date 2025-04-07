using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Validators;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.AddOn
{
    public interface IAddOnMapper
    {
        Task<AddOnPlan> GetAddOnFreePlanAsync();
        AddOnPlanUser MapToPlanUser(int userId, int addOnPlanId, int? billingCreditId);
        Task<int> CreateAddOnPlanUserAsync(AddOnPlanUser addOnPlanUser);
        Task<ValidationResult> CanProceedToBuy(BuyAddOnPlan buyAddOnPlan, int userId, UserBillingInformation userBillingInformation, AccountTypeEnum accountType);
        Task ProceedToBuy(User user, BuyAddOnPlan buyAddOnPlan, UserBillingInformation userBillingInformation, Model.BillingCredit currentBillingCredit,
                            CreditCardPayment payment, PlanAmountDetails amountDetails, UserBillingInformation userOrClientManagerBillingInformation,
                            AccountTypeEnum accountType);
        Task<SapBillingDto> MapAddOnBillingToSapAsync(SapSettings sapSettings, User user, BuyAddOnPlan buyAddOnPlan, string cardNumber, string holderName,
                                                        Model.BillingCredit billingCredit, string authorizationNumber, int invoiceId,
                                                        UserBillingInformation userOrClientManagerBillingInformation, AccountTypeEnum accountType);
    }
}
