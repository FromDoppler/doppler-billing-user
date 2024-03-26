using Dapper;
using Doppler.BillingUser.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class LandingPlanUserRepository : ILandingPlanUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public LandingPlanUserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateLandingPlanUserAsync(LandingPlanUser landingPlanUser)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryAsync<int>(@"
INSERT INTO [dbo].[LandingPlanUser]
            ([IdUser]
            ,[IdLandingPlan]
            ,[IdBillingCredit]
            ,[PackQty]
            ,[Fee]
            ,[CreatedAt])
VALUES
    (@idUser,
    @idLandingPlan,
    @idBillingCredit,
    @packQty,
    @fee,
    @createdAt);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @idUser = landingPlanUser.IdUser,
                @idLandingPlan = landingPlanUser.IdLandingPlan,
                @idBillingCredit = landingPlanUser.IdBillingCredit,
                @packQty = landingPlanUser.PackQty,
                @fee = landingPlanUser.Fee,
                @createdAt = DateTime.UtcNow
            });

            return result.FirstOrDefault();
        }
    }
}
