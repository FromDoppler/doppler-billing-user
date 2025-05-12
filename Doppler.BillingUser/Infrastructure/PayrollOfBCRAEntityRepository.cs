using Dapper;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class PayrollOfBCRAEntityRepository : IPayrollOfBCRAEntityRepository
    {
        private readonly IDatabaseConnectionFactory connectionFactory;

        public PayrollOfBCRAEntityRepository(IDatabaseConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public async Task<PayrollOfBCRAEntity> GetByBankCode(string bankCode)
        {
            using var connection = connectionFactory.GetConnection();
            var payrollOfBCRAEntity = await connection.QueryFirstOrDefaultAsync<PayrollOfBCRAEntity>(@"
SELECT [BankCode],[BankName]
FROM [dbo].[PayrollOfBCRAEntity]
WHERE [BankCode] = @bankCode", new
            {
                bankCode
            });

            return payrollOfBCRAEntity;
        }
    }
}
