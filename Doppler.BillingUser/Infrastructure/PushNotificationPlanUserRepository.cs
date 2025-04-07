using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class PushNotificationPlanUserRepository : IPushNotificationPlanUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public PushNotificationPlanUserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CurrentPlan> GetCurrentPlan(string accountname)
        {
            using var connection = _connectionFactory.GetConnection();
            var currentPlan = await connection.QueryFirstOrDefaultAsync<CurrentPlan>(@"
SELECT
    PNP.[IdPushNotificationPlan] AS IdPlan,
    PNP.[Description],
    PNP.[Quantity],
    PNP.[Fee],
    PNP.[Description],
    BC.IdPaymentMethod AS PaymentMethod
FROM [UserAddOn] UA
INNER JOIN [BillingCredits] BC ON BC.IdBillingCredit = UA.IdCurrentBillingCredit
INNER JOIN [PushNotificationPlanUser] PNPU ON PNPU.IdBillingCredit = BC.IdBillingCredit
INNER JOIN [PushNotificationPlan] PNP ON PNP.IdPushNotificationPlan = PNPU.IdPushNotificationPlan
WHERE
    UA.[IdUser] = (SELECT IdUser FROM [User] WHERE Email = @accountname) AND UA.IdAddOnType = @addOnType AND
    BC.IdBillingCreditType != @idBillingCreditType", new { accountname, addOnType = AddOnType.PushNotification, idBillingCreditType = (int)BillingCreditTypeEnum.PushNotification_Canceled });

            return currentPlan;
        }
    }
}
