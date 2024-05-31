using Dapper;
using Doppler.BillingUser.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class LandingPlanRepository : ILandingPlanRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public LandingPlanRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IList<LandingPlan>> GetAll()
        {
            using var connection = _connectionFactory.GetConnection();
            var landingPlans = await connection.QueryAsync<LandingPlan>(@"
SELECT
    LP.IdLandingPlan,
    LP.Description,
    LP.LandingQty,
    LP.Fee
FROM [LandingPlan] LP;");

            return landingPlans.ToList();
        }
    }
}
