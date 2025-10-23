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

        public async Task<TimesApplyedPromocode> GetHowManyTimesApplyedPromocode(string code, string accountName, int planType)
        {
            using var _ = _timeCollector.StartScope();
            using var connection = _connectionFactory.GetConnection();
            var times = await connection.QueryFirstOrDefaultAsync<TimesApplyedPromocode>(@"
SELECT
    MAX(MONTH(B.Date)) AS LastMonthApplied,
    MAX(YEAR(B.Date)) AS LastYearApplied,
    COUNT(DISTINCT MONTH(B.Date)) AS CountApplied
FROM
    [BillingCredits] B  WITH(NOLOCK)
INNER JOIN [User] U  WITH(NOLOCK) ON U.IdUser = B.IdUser
INNER JOIN [Promotions] P  WITH(NOLOCK) ON  P.IdPromotion = B.IdPromotion
WHERE
    U.Email = @email AND
    U.IdCurrentBillingCredit IS NOT NULL AND
    P.Code = @code AND
    B.DiscountPlanFeePromotion IS NOT NULL AND
    ((@planType = 1 AND B.IdBillingCreditType IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)) OR --Email Marketing
    (@planType = 2 AND B.IdBillingCreditType IN (28, 29, 30, 31, 32)) OR --Chat
    (@planType = 3 AND B.IdBillingCreditType IN (23, 24, 25, 26, 27)) OR --Landing
    (@planType = 4 AND B.IdBillingCreditType IN (34, 35, 36, 37, 38)) OR --OnSite
    (@planType = 5 AND B.IdBillingCreditType IN (40, 41, 42, 43, 44))) --Push Notification",
                new
                {
                    code,
                    email = accountName,
                    planType
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

        public async Task<Promotion> GetAddOnPromotionByCodeAndAddOnType(string code, int addOnTypeId)
        {
            using var _ = _timeCollector.StartScope();
            using var connection = _connectionFactory.GetConnection();

            var promotion = await connection.QueryFirstOrDefaultAsync<Promotion>(@"
SELECT
    AP.[IdPromotion],
    AP.[CreationDate],
    [ExpiringDate],
    [TimesUsed],
    [TimesToUse],
    AP.[Code],
    AP.[Active],
    AP.Discount as DiscountPercentage,
    AP.[Duration]
FROM [AddOnPromotion] AP  WITH(NOLOCK)
INNER JOIN [Promotions] P WITH(NOLOCK) ON P.IdPromotion = AP.IdPromotion
WHERE
    AP.[Code] = @code AND
    AP.[IdAddOnType] = @addOnTypeId AND
    (AP.[Active] = 1) AND
    ([TimesToUse] is null OR [TimesToUse] > [TimesUsed]) AND
    ([ExpiringDate] is null OR [ExpiringDate] >= @now)",
                new
                {
                    code,
                    addOnTypeId,
                    @now = DateTime.Now
                });

            return promotion;
        }
    }
}
