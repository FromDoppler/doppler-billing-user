using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class ChatPlanUserRepository : IChatPlanUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public ChatPlanUserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CurrentPlan> GetCurrentPlan(string accountname)
        {
            using var connection = _connectionFactory.GetConnection();
            var currentPlan = await connection.QueryFirstOrDefaultAsync<CurrentPlan>(@"
SELECT
    CP.[IdChatPlan] AS IdPlan,
    CP.[Description],
    CP.[ConversationQty],
    CP.[Fee],
    CP.[Description],
    BC.IdPaymentMethod AS PaymentMethod
FROM [UserAddOn] UA
INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = UA.IdCurrentBillingCredit
INNER JOIN [ChatPlanUsers] CPU ON CPU.IdBillingCredit = BC.IdBillingCredit
INNER JOIN [ChatPlans] CP ON CP.IdChatPlan = CPU.IdChatPlan
WHERE
    UA.[IdUser] = (SELECT IdUser FROM [User] WHERE Email = @accountname) AND UA.IdAddOnType = @addOnType AND
    BC.IdBillingCreditType != @idBillingCreditType", new { accountname, addOnType = AddOnType.Chat, idBillingCreditType = (int)BillingCreditTypeEnum.Conversation_Canceled });

            return currentPlan;
        }
    }
}
