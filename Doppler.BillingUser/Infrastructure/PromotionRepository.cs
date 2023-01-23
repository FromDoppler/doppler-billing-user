using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.Infrastructure
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public PromotionRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task IncrementUsedTimes(Promotion promocode)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.ExecuteAsync(@"
UPDATE
    [Promotions]
SET
    [TimesUsed] = [TimesUsed] + 1
WHERE [IdPromotion] = @promocodeId", new
            {
                @promocodeId = promocode.IdPromotion
            });
        }

        public async Task DecrementUsedTimesAsync(Promotion promocode)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.ExecuteAsync(@"
UPDATE
    [Promotions]
SET
    [TimesUsed] = [TimesUsed] - 1
WHERE [IdPromotion] = @promocodeId", new
            {
                @promocodeId = promocode.IdPromotion
            });
        }

        public async Task<Promotion> GetById(int promocodeId)
        {
            using var connection = _connectionFactory.GetConnection();
            var promotion = await connection.QueryFirstOrDefaultAsync<Promotion>(@"
SELECT
    [IdPromotion],
    [ExtraCredits],
    [DiscountPlanFee] AS DiscountPercentage,
    [Code],
    [Duration]
FROM
    [Promotions]
WHERE [IdPromotion] = @promocodeId", new
            {
                promocodeId
            });

            return promotion;
        }

        public async Task<TimesApplyedPromocode> GetHowManyTimesApplyedPromocode(string code, string accountName)
        {
            using var connection = _connectionFactory.GetConnection();
            var times = await connection.QueryFirstOrDefaultAsync<TimesApplyedPromocode>(@"
SELECT
    COUNT(DISTINCT MONTH(B.Date)) AS CountApplied
FROM
    [BillingCredits] B
INNER JOIN [User] U ON U.IdUser = B.IdUser
INNER JOIN [Promotions] P ON  P.IdPromotion = B.IdPromotion
WHERE
    U.Email = @email AND
    U.IdCurrentBillingCredit IS NOT NULL AND
    P.Code = @code AND
    B.DiscountPlanFeePromotion IS NOT NULL",
                new
                {
                    code,
                    email = accountName
                });

            return times;
        }
    }
}
