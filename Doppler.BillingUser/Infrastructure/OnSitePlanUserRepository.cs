using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class OnSitePlanUserRepository : IOnSitePlanUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public OnSitePlanUserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CurrentPlan> GetCurrentPlan(string accountname)
        {
            using var connection = _connectionFactory.GetConnection();
            var currentPlan = await connection.QueryFirstOrDefaultAsync<CurrentPlan>(@"
SELECT
    OSP.[IdOnSitePlan] AS IdPlan,
    OSP.[Description],
    OSP.[PrintQty],
    OSP.[PrintQty] as Quantity,
    OSP.[Fee],
    OSP.[Description],
    BC.IdPaymentMethod AS PaymentMethod
FROM [UserAddOn] UA
INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = UA.IdCurrentBillingCredit
INNER JOIN [OnSitePlanUser] OSPU ON OSPU.IdBillingCredit = BC.IdBillingCredit
INNER JOIN [OnSitePlan] OSP ON OSP.IdOnSitePlan = OSPU.IdOnSitePlan
WHERE
    UA.[IdUser] = (SELECT IdUser FROM [User] WHERE Email = @accountname) AND UA.IdAddOnType = @addOnType AND
    BC.IdBillingCreditType != @idBillingCreditType", new { accountname, addOnType = AddOnType.OnSite, idBillingCreditType = (int)BillingCreditTypeEnum.OnSite_Canceled });

            return currentPlan;
        }
    }
}
