using Dapper;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Extensions;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.TimeCollector;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class UserPaymentHistoryRepository : IUserPaymentHistoryRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ITimeCollector _timeCollector;

        public UserPaymentHistoryRepository(IDatabaseConnectionFactory connectionFactory, ITimeCollector timeCollector)
        {
            _connectionFactory = connectionFactory;
            _timeCollector = timeCollector;
        }

        public async Task<int> CreateUserPaymentHistoryAsync(UserPaymentHistory userPaymentHistory)
        {
            using var _ = _timeCollector.StartScope();
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
    [Date],
    [CreditCardLastFourDigits])
VALUES (
    @idUser,
    @idPaymentMethod,
    @idUserTypePlan,
    @idBillingCredit,
    @status,
    @source,
    @errorMessage,
    @date,
    @creditCardLastFourDigits);
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
                @errorMessage = userPaymentHistory.ErrorMessage,
                @creditCardLastFourDigits = userPaymentHistory.CreditCardLastFourDigits
            });

            return result;
        }


        public async Task<int> GetAttemptsToUpdateAsync(int idUser, DateTime from, DateTime to, string source)
        {
            using var _ = _timeCollector.StartScope();
            using var connection = _connectionFactory.GetConnection();
            var result = await connection.QueryFirstOrDefaultAsync<int>(@"
SELECT COUNT(DISTINCT CreditCardLastFourDigits) FROM [dbo].[UserPaymentHistory]
WHERE IdUser = @idUser
AND (Date BETWEEN @from AND @to)
AND Source = @source
AND Status = @status",
            new
            {
                @from = from,
                @to = to,
                @idUser = idUser,
                @status = PaymentStatusEnum.DeclinedPaymentTransaction.ToDescription(),
                @source = source,
            });

            return result;
        }
    }
}
