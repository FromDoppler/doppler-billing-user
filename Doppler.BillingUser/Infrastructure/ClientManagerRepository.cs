using Dapper;
using Doppler.BillingUser.ExternalServices.FirstData;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class ClientManagerRepository(IDatabaseConnectionFactory connectionFactory) : IClientManagerRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory = connectionFactory;

        public async Task<CreditCard> GetEncryptedCreditCard(int idClientManager)
        {
            using var connection = _connectionFactory.GetConnection();
            var encryptedCreditCard = await connection.QueryFirstOrDefaultAsync<CreditCard>(@"
SELECT
    CCHolderFullName as HolderName,
    CCNumber as Number,
    CCExpMonth as ExpirationMonth,
    CCExpYear as ExpirationYear,
    CCVerification as Code,
    IdCCType as CardType
FROM
    [ClientManager]
WHERE
    IdClientManager = @idClientManager;",
                new
                {
                    idClientManager
                });

            return encryptedCreditCard;
        }
    }
}
