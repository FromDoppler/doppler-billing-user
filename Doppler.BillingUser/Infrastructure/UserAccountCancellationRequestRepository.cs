using Dapper;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class UserAccountCancellationRequestRepository : IUserAccountCancellationRequestRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserAccountCancellationRequestRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> SaveRequestAsync(int userId, string contactName, string accountCancellationReason, string contactPhone, string contactSchedule)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[UserAccountCancellationRequest]
    ([ContactName]
    ,[AccountCancellatioReason]
    ,[ContactPhone]
    ,[ContactSchedule]
    ,[IdUser]
    ,[CreatedAt])
VALUES
    (@contactName
    ,@accountCancellationReason
    ,@contactPhone
    ,@contactSchedule
    ,@userId
    ,@date);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @date = DateTime.UtcNow,
                contactName,
                accountCancellationReason,
                contactPhone,
                contactSchedule,
                userId
            });

            return result;
        }
    }
}
