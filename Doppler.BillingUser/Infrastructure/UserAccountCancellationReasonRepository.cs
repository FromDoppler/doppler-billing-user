using Dapper;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class UserAccountCancellationReasonRepository : IUserAccountCancellationReasonRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserAccountCancellationReasonRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserAccountCancellationReason> GetById(int userAccountCancellationReasonId)
        {
            using var connection = _connectionFactory.GetConnection();
            var accountCancellationReason = await connection.QueryFirstOrDefaultAsync<UserAccountCancellationReason>(@"
SELECT UACR.IdUserAccountCancellationReason AS UserAccountCancellationReasonId, R.DescriptionEs, R.DescriptionEn
FROM [dbo].[UserAccountCancellationReason] UACR
INNER JOIN [dbo].[Resource] R ON R.IdResource = UACR.IdResource
WHERE UACR.IdUserAccountCancellationReason = @userAccountCancellationReasonId", new { userAccountCancellationReasonId });

            return accountCancellationReason;
        }
    }
}
