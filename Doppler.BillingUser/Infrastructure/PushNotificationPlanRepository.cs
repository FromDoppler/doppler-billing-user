using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class PushNotificationPlanRepository : IPushNotificationPlanRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public PushNotificationPlanRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<PushNotificationPlan> GetById(int pushNotificationPlanId)
        {
            using var connection = _connectionFactory.GetConnection();
            var pushNotificationPlan = await connection.QueryFirstOrDefaultAsync<PushNotificationPlan>(@"
SELECT [IdPushNotificationPlan],
        [Description],
        [Quantity],
        [Fee],
        [FreeDays]
FROM [dbo].[PushNotificationPlan]
WHERE [IdPushNotificationPlan] = @pushNotificationPlanId", new { pushNotificationPlanId });

            return pushNotificationPlan;
        }

        public async Task<PushNotificationPlan> GetFreePlan()
        {
            using var connection = _connectionFactory.GetConnection();
            var pushNotificationPlan = await connection.QueryFirstOrDefaultAsync<PushNotificationPlan>(@"
SELECT [IdPushNotificationPlan],
        [Description],
        [Quantity],
        [Fee],
        [FreeDays]
FROM [dbo].[PushNotificationPlan]
WHERE [Fee] = 0");

            return pushNotificationPlan;
        }
    }
}
