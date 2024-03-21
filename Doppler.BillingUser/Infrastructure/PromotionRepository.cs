using System;
using System.Numerics;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Connections;
using Doppler.BillingUser.TimeCollector;

namespace Doppler.BillingUser.Infrastructure
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ITimeCollector _timeCollector;

        public PromotionRepository(IDatabaseConnectionFactory connectionFactory, ITimeCollector timeCollector)
        {
            _connectionFactory = connectionFactory;
            _timeCollector = timeCollector;
        }

        public async Task IncrementUsedTimes(Promotion promocode)
        {
            using var _ = _timeCollector.StartScope();
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
            using var _ = _timeCollector.StartScope();
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
            using var _ = _timeCollector.StartScope();
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

        public async Task<Promotion> GetPromotionByCode(string code, int userType, int planId)
        {
            using var connection = _connectionFactory.GetConnection();

            var promotion = await connection.QueryFirstOrDefaultAsync<Promotion>(@"
SELECT
    [IdPromotion],
    [IdUserTypePlan],
    [CreationDate],
    [ExpiringDate],
    [TimesUsed],
    [TimesToUse],
    [Code],
    [ExtraCredits],
    [Active],
    [DiscountPlanFee] as DiscountPercentage,
    [AllPlans],
    [AllSubscriberPlans],
    [AllPrepaidPlans],
    [AllMonthlyPlans],
    [Duration]
FROM
    [Promotions]  WITH(NOLOCK)
WHERE
    [Code] = @code AND
    [Active] = 1 AND
    ([TimesToUse] is null OR [TimesToUse] > [TimesUsed]) AND
    ([ExpiringDate] is null OR [ExpiringDate] >= @now) AND
    ([IdUserTypePlan] = @planId OR
    [AllPlans] = 1 OR
    (@userType = @individual AND [AllPrepaidPlans] = 1) OR
    (@userType = @subscribers AND [AllSubscriberPlans] = 1) OR
    (@userType = @monthly AND [AllMonthlyPlans] = 1))",
                new
                {
                    code,
                    planId,
                    userType,
                    @individual = (int)UserTypeEnum.INDIVIDUAL,
                    @subscribers = (int)UserTypeEnum.SUBSCRIBERS,
                    @monthly = (int)UserTypeEnum.MONTHLY,
                    @now = DateTime.Now
                });

            return promotion;
        }
    }
}
