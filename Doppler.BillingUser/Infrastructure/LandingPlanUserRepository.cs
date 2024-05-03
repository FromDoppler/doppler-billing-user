using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System;
using System.Collections.Generic;
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

        public async Task<IList<LandingPlanUser>> GetLandingPlansByUserIdAndBillingCreditIdAsync(int userId, int billingCreditId)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryAsync<LandingPlanUser>(@"
SELECT [IdLandingPlanUser]
        ,[IdUser]
        ,[IdLandingPlan]
        ,[IdBillingCredit]
        ,[PackQty]
        ,[Fee]
        ,[CreatedAt] AS Created
FROM [dbo].[LandingPlanUser]
WHERE [IdUser] = @idUser AND [IdBillingCredit] = @idBillingCredit",
            new
            {
                @idUser = userId,
                @idBillingCredit = billingCreditId
            });

            return result.ToList();
        }
    }
}
