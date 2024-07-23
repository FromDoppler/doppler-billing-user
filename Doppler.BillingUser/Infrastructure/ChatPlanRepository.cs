using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class ChatPlanRepository : IChatPlanRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public ChatPlanRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<ChatPlan> GetById(int chatPlanId)
        {
            using var connection = _connectionFactory.GetConnection();
            var chatPlan = await connection.QueryFirstOrDefaultAsync<ChatPlan>(@"
SELECT [IdChatPlan],
        [Description],
        [ConversationQty],
        [Fee]
FROM [dbo].[ChatPlans]
WHERE [IdChatPlan] = @chatPlanId", new { chatPlanId });

            return chatPlan;
        }
    }
}
