using Dapper;
using Doppler.BillingUser.Model;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class BillingRepository : IBillingRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        public BillingRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<BillingInformation> GetBillingInformation(string email)
        {
            using (IDbConnection connection = await _connectionFactory.GetConnection())
            {
                var results = await connection.QueryAsync<BillingInformation>(@"
SELECT
    U.BillingFirstName AS Firstname,
    U.BillingLastName AS Lastname,
    U.BillingAddress AS Address,
    U.BillingCity AS City,
    isnull(S.StateCode, '') AS Province,
    isnull(CO.Code, '') AS Country,
    U.BillingZip AS ZipCode,
    U.BillingPhone AS Phone
FROM
    [User] U
    LEFT JOIN [State] S ON U.IdBillingState = S.IdState
    LEFT JOIN [Country] CO ON S.IdCountry = CO.IdCountry
WHERE
    U.Email = @email",
                    new { email });
                return results.FirstOrDefault();
            }
        }
    }
}
