using Dapper;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
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

        public async Task<UserBillingInformation> GetUserBillingInformation(int idClientManager)
        {
            using var connection = _connectionFactory.GetConnection();
            var user = await connection.QueryFirstOrDefaultAsync<UserBillingInformation>(@"
SELECT
    CM.IdClientManager AS IdUser,
    CM.IdPaymentMethod as PaymentMethod,
    0 as ResponsableIVA,
    CM.PaymentType as PaymentType,
    CM.PaymentWay as PaymentWay,
    CM.BankName as BankName,
    CM.BankAccount as BankAccount,
    CM.CFDIUse as CFDIUse,
    CM.Email,
    CMS.IdCountry as IdBillingCountry,
    CM.UTCFirstPayment,
    '' AS OriginInbound,
    CM.CUIT,
    CM.[UTCFirstPayment] AS UTCUpgrade,
    0 AS IdCurrentBillingCredit,
    0 AS MaxSubscribers,
    CM.IsCancelated,
    0 AS UpgradePending,
    0 AS TaxRegime,
    '' AS Cbu
FROM ClientManager CM
LEFT JOIN State CMS ON CM.IdBillingState = CMS.IdState
WHERE CM.IdClientManager =  @idClientManager;",
                new
                {
                    idClientManager
                });

            return user;
        }

        public async Task<User> GetUserInformation(string accountName)
        {
            using var connection = _connectionFactory.GetConnection();
            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    CM.IdClientManager AS IdUser,
    CM.Email,
    CM.FirstName,
    CM.LastName,
    CM.Address,
    CM.BillingPhone,
    CM.Company,
    CM.BillingCity,
    BS.Name as BillingStateName,
    CM.ZipCode,
    L.Name as Language,
    C.Name as BillingCountryName,
    V.Fullname as Vendor,
    CM.CUIT,
    CM.RazonSocial,
    CM.IdConsumerType,
    CM.BillingEmails,
    BS.IdCountry as IdBillingCountry,
    CM.IsCancelated,
    CM.Company
FROM [ClientManager] CM
LEFT JOIN [Vendor] V ON V.IdVendor = CM.IdVendor
LEFT JOIN [State] BS ON BS.IdState = CM.IdBillingState
LEFT JOIN [Country] C ON C.IdCountry = BS.IdCountry
LEFT JOIN [Language] L ON CM.IdLanguage = L.IdLanguage
WHERE CM.Email = @accountName;",
                new
                {
                    accountName
                });

            return user;
        }
    }
}
