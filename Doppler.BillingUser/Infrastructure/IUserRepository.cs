using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserRepository
    {
        void UnblockAccountNotPayed(string accountname);
        Task<UserBillingInformation> GetUserBillingInformation(string accountName);
        Task<UserTypePlanInformation> GetUserCurrentTypePlan(int idUser);
        Task<CreditCard> GetEncryptedCreditCard(string accountName);
        Task<UserTypePlanInformation> GetUserNewTypePlan(int idUserTypePlan);
        Task<int> UpdateUserBillingCredit(UserBillingInformation user);
        Task<int> GetAvailableCredit(int idUser);
        Task<User> GetUserInformation(string accountName);
        Task<int> UpdateUserPurchaseIntentionDate(int idUser);
        Task<int> GetCurrentMonthlyAddedEmailsWithBillingAsync(int idUser);
        Task CancelUser(int idUser, int idAccountCancelationReason, string cancelatedObservation);
        Task<ConversationPlanInformation> GetConversationPlan(int idConversationPlan);
    }
}
