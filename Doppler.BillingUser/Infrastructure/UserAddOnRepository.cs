using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.TimeCollector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class UserAddOnRepository : IUserAddOnRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ITimeCollector _timeCollector;

        public UserAddOnRepository(IDatabaseConnectionFactory connectionFactory, ITimeCollector timeCollector)
        {
            _connectionFactory = connectionFactory;
            _timeCollector = timeCollector;
        }

        public async Task<IList<UserAddOn>> GetAllByUserIdAsync(int userId)
        {
            using var connection = _connectionFactory.GetConnection();
            var userAddOns = await connection.QueryAsync<UserAddOn>(@"SELECT [IdUserAddOn]
        ,[IdUser]
        ,[IdAddOnType]
        ,[IdCurrentBillingCredit]
    FROM [dbo].[UserAddOn]
    WHERE IdUser = @userId", new { userId });

            return userAddOns.ToList();
        }

        public async Task<UserAddOn> GetByUserIdAndAddOnType(int userId, int addOnType)
        {
            using var connection = _connectionFactory.GetConnection();
            UserAddOn userAddOn = await connection.QueryFirstOrDefaultAsync<UserAddOn>(@"SELECT [IdUserAddOn]
        ,[IdUser]
        ,[IdAddOnType]
        ,[IdCurrentBillingCredit]
    FROM [dbo].[UserAddOn]
    WHERE IdUser = @userId AND IdAddOnType = @addOnType", new { userId, addOnType });

            return userAddOn;
        }

        public async Task SaveCurrentBillingCreditByUserIdAndAddOnTypeAsync(int userId, int addOnType, int? billingCreditId)
        {
            using var _ = _timeCollector.StartScope();
            using var connection = _connectionFactory.GetConnection();
            await connection.ExecuteAsync(@"
IF NOT EXISTS(SELECT * FROM dbo.UserAddOn WHERE IdUser = @userId AND [IdAddOnType] = @addOnType)
BEGIN
    INSERT INTO [dbo].[UserAddOn] ([IdUser], [IdAddOnType], [IdCurrentBillingCredit])
    VALUES (@userId, @addOnType, @billingCreditId)
END
ELSE BEGIN
    UPDATE [dbo].[UserAddOn]
    SET [IdCurrentBillingCredit] = @billingCreditId, [LastUpdate] = @lastUpdate
WHERE [IdUser] = @userId AND [IdAddOnType] = @addOnType
END", new
            {
                userId,
                addOnType,
                billingCreditId,
                lastUpdate = DateTime.UtcNow
            });
        }
    }
}
