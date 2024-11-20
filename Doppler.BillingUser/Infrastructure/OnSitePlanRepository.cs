using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class OnSitePlanRepository : IOnSitePlanRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public OnSitePlanRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<OnSitePlan> GetById(int onSitePlanid)
        {
            using var connection = _connectionFactory.GetConnection();
            var onSitePlan = await connection.QueryFirstOrDefaultAsync<OnSitePlan>(@"
SELECT [IdOnSitePlan],
        [Description],
        [PrintQty],
        [Fee]
FROM [dbo].[OnSitePlan]
WHERE [IdOnSitePlan] = @onSitePlanid", new { onSitePlanid });

            return onSitePlan;
        }
    }
}