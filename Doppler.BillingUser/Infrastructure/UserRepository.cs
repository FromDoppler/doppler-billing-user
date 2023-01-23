using Dapper;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class UserRepository : IUserRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public UserRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async void UnblockAccountNotPayed(string accountname)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.QueryAsync(@"
UPDATE [User]
SET [BlockedAccountNotPayed]=0
WHERE [Email] = @email;
", new
            {
                @email = accountname
            }
            );
        }

        public async Task<UserBillingInformation> GetUserBillingInformation(string accountName)
        {
            using var connection = _connectionFactory.GetConnection();
            var user = await connection.QueryFirstOrDefaultAsync<UserBillingInformation>(@"
SELECT
    U.IdUser,
    U.PaymentMethod,
    U.ResponsableIVA,
    U.PaymentType,
    U.PaymentWay,
    U.BankName,
    U.BankAccount,
    U.CFDIUse,
    U.Email,
    S.IdCountry as IdBillingCountry,
    U.UTCFirstPayment,
    U.OriginInbound,
    U.CUIT,
    U.UTCUpgrade,
    U.IdCurrentBillingCredit,
    U.MaxSubscribers,
    U.IsCancelated,
    U.UpgradePending
FROM
    [User] U
    INNER JOIN
        State S ON U.IdBillingState = S.IdState
WHERE
    U.Email = @email;",
                new
                {
                    @email = accountName
                });

            return user;
        }

        public async Task<UserTypePlanInformation> GetUserCurrentTypePlan(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();
            var userTypePlan = await connection.QueryFirstOrDefaultAsync<UserTypePlanInformation>(@"
SELECT TOP 1
    UTP.[IdUserTypePlan],
    UTP.[IdUserType],
    UTP.[EmailQty],
    UTP.[Fee],
    UTP.[ExtraEmailCost],
    UTP.[SubscribersQty],
    BC.IdPaymentMethod AS PaymentMethod
FROM
    [dbo].[BillingCredits] BC
    INNER JOIN
    [dbo].[UserTypesPlans] UTP
    ON
    BC.IdUserTypePlan = UTP.IdUserTypePlan
INNER JOIN
    [dbo].[User] U
    ON
    U.[IdUser] = BC.[IdUser]
WHERE
    BC.[IdUser] = @idUser AND U.UpgradePending IS NOT NULL
ORDER BY
    BC.[Date] DESC;",
                new
                {
                    idUser
                });

            return userTypePlan;
        }

        public async Task<CreditCard> GetEncryptedCreditCard(string accountName)
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
    [User]
WHERE
    Email = @email;",
                new
                {
                    @email = accountName
                });

            return encryptedCreditCard;
        }

        public async Task<UserTypePlanInformation> GetUserNewTypePlan(int idUserTypePlan)
        {
            using var connection = _connectionFactory.GetConnection();
            var userTypePlan = await connection.QueryFirstOrDefaultAsync<UserTypePlanInformation>(@"
SELECT
    [IdUserTypePlan],
    [IdUserType],
    [EmailQty],
    [Fee],
    [ExtraEmailCost],
    [SubscribersQty],
    [Description] as Subscribers
FROM
    [dbo].[UserTypesPlans]
WHERE
    [IdUserTypePlan] = @idUserTypePlan;",
                new
                {
                    @idUserTypePlan = idUserTypePlan
                });

            return userTypePlan;
        }

        public async Task<int> UpdateUserBillingCredit(UserBillingInformation user)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.ExecuteAsync(@"
UPDATE
    [dbo].[User]
SET
    CUIT = @cuit,
    UTCFirstPayment = @utfFirstPayment,
    IdCurrentBillingCredit = @idCurrentBillingCredit,
    OriginInbound = @originInbound,
    UpgradePending = @upgradePending,
    UTCUpgrade = @utcUpgrade,
    MaxSubscribers = @maxSubscribers
WHERE
    IdUser = @idUser;",
            new
            {
                @idUser = user.IdUser,
                @IdCurrentBillingCredit = user.IdCurrentBillingCredit,
                @cuit = user.Cuit,
                @utfFirstPayment = user.UTCFirstPayment,
                @originInbound = user.OriginInbound,
                @upgradePending = user.UpgradePending,
                @utcUpgrade = user.UTCUpgrade,
                @maxSubscribers = user.MaxSubscribers
            });

            return result;
        }

        public async Task<int> GetAvailableCredit(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();
            var partialBalance = await connection.QueryFirstOrDefaultAsync<int>(@"
SELECT
    PartialBalance
FROM
    [dbo].[MovementsCredits]
WHERE
    IdUser = @idUser
ORDER BY
    IdMovementCredit
DESC",
                new
                {
                    @idUser = idUser
                });

            return partialBalance;
        }

        public async Task<User> GetUserInformation(string accountName)
        {
            using var connection = _connectionFactory.GetConnection();
            var user = await connection.QueryFirstOrDefaultAsync<User>(@"
SELECT
    U.IdUser,
    U.Email,
    U.FirstName,
    U.LastName,
    U.Address,
    U.PhoneNumber,
    U.Company,
    U.CityName,
    BS.Name as BillingStateName,
    U.ZipCode,
    L.Name as Language,
    C.Name as BillingCountryName,
    V.Fullname as Vendor,
    U.CUIT,
    U.RazonSocial,
    U.IdConsumerType,
    U.BillingEmails,
    BS.IdCountry as IdBillingCountry,
    U.IsCancelated
FROM
    [User] U
LEFT JOIN
    [Vendor] V ON V.IdVendor = U.IdVendor
LEFT JOIN
    [State] BS ON BS.IdState = U.IdBillingState
LEFT JOIN
    [Country] C ON C.IdCountry = BS.IdCountry
INNER JOIN
    Language L ON U.IdLanguage = L.IdLanguage
WHERE
    U.Email = @accountName;",
                new
                {
                    accountName
                });

            return user;
        }

        public async Task<int> UpdateUserPurchaseIntentionDate(string accountName)
        {
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.ExecuteAsync(@"
UPDATE
    [dbo].[User]
SET
    LastPurchaseIntentionDate = @date
WHERE
    Email = @accountName;",
            new
            {
                accountName,
                @date = DateTime.UtcNow
            });

            return result;
        }

        public async Task<int> GetCurrentMonthlyAddedEmailsWithBillingAsync(int idUser)
        {
            using var connection = _connectionFactory.GetConnection();
            var partialBalance = await connection.QueryFirstOrDefaultAsync<int>(@"
SELECT SUM(CreditsQty)
FROM MovementsCredits
WHERE IdUser = @idUser AND MONTH(GETDATE()) = MONTH(Date) AND YEAR(GETDATE()) = YEAR(Date) AND
    CreditsQty > 0 AND IdUserType = 2 AND IdBillingCredit IS NOT NULL",
                new
                {
                    @idUser = idUser
                });

            return partialBalance;
        }


        public async Task CancelUser(int idUser, int idAccountCancelationReason, string cancelatedObservation)
        {
            using var connection = _connectionFactory.GetConnection();
            await connection.QueryAsync(@"
UPDATE [User]
SET IsCancelated = 1,
IdAccountCancellationReason = @idAccountCancelationReason,
CancelatedObservation = @cancelatedObservation,
CancelatedDate = @date
WHERE IdUser = @IdUser;", new
            {
                @idUser = idUser,
                @idAccountCancelationReason = idAccountCancelationReason,
                @date = DateTime.UtcNow,
                @cancelatedObservation = cancelatedObservation
            }
            );
        }
    }
}
