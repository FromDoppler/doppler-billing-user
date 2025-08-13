using Dapper;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class AccountCancellationReasonRepository : IAccountCancellationReasonRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public AccountCancellationReasonRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<AccountCancellationReason> GetById(int accountCancellationReasonId)
        {
            using var connection = _connectionFactory.GetConnection();
            var accountCancellationReason = await connection.QueryFirstOrDefaultAsync<AccountCancellationReason>(@"
SELECT [IdAccountCancellationReason] AS AccountCancellationReasonId, [SendEmailToUser], [Active]
FROM [dbo].[AccountCancellationReason]
WHERE [IdAccountCancellationReason] = @accountCancellationReasonId", new { accountCancellationReasonId });

            return accountCancellationReason;
        }
    }
}
