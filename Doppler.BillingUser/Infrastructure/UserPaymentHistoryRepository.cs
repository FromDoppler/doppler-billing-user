using Dapper;
using Doppler.BillingUser.Model;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class UserPaymentHistoryRepository : IUserPaymentHistoryRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserPaymentHistoryRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateUserPaymentHistoryAsync(UserPaymentHistory userPaymentHistory)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryFirstOrDefaultAsync<int>(@"
INSERT INTO [dbo].[UserPaymentHistory]
    ([IdUser],
    [IdPaymentMethod],
    [IdUserTypePlan],
    [IdBillingCredit],
    [Status],
    [Source],
    [ErrorMessage],
    [Date])
VALUES (
    @idUser,
    @idPaymentMethod,
    @idUserTypePlan,
    @idBillingCredit,
    @status,
    @source,
    @errorMessage,
    @date);
SELECT CAST(SCOPE_IDENTITY() AS INT)",
            new
            {
                @date = DateTime.UtcNow,
                @idUser = userPaymentHistory.IdUser,
                @idPaymentMethod = userPaymentHistory.IdPaymentMethod,
                @idUserTypePlan = userPaymentHistory.IdPlan,
                @idBillingCredit = userPaymentHistory.IdBillingCredit,
                @status = userPaymentHistory.Status,
                @source = userPaymentHistory.Source,
                @errorMessage = userPaymentHistory.ErrorMessage
            });

            return result;
        }
    }
}
