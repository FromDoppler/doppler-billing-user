using Dapper;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.TimeCollector;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ITimeCollector _timeCollector;

        public CurrencyRepository(IDatabaseConnectionFactory connectionFactory, ITimeCollector timeCollector)
        {
            _connectionFactory = connectionFactory;
            _timeCollector = timeCollector;
        }

        public async Task<decimal> GetCurrencyRateAsync(int idCurrencyTypeFrom, int idCurrencyTypeTo, DateTime date)
        {
            using var _ = _timeCollector.StartScope();
            using var connection = _connectionFactory.GetConnection();
            var rate = await connection.QueryFirstOrDefaultAsync<CurrencyRate>(@"
SELECT
    [IdCurrencyRate],
    [IdCurrencyTypeFrom],
    [IdCurrencyTypeTo],
    [Rate],
    [UTCFromDate],
    [Active]
FROM
    [CurrencyRate] R
WHERE
    ((R.IdCurrencyTypeFrom = @idCurrencyTypeFrom AND R.IdCurrencyTypeTo = idCurrencyTypeTo) OR
    (R.IdCurrencyTypeFrom = idCurrencyTypeTo AND R.IdCurrencyTypeTo = @idCurrencyTypeFrom)) AND
    R.UTCFromDate <= @date
ORDER BY R.UTCFromDate DESC",
                new
                {
                    idCurrencyTypeFrom,
                    idCurrencyTypeTo,
                    date
                });

            if (rate == null)
            {
                return 1;
            }

            if (rate.IdCurrencyTypeFrom == idCurrencyTypeFrom && rate.IdCurrencyTypeTo == idCurrencyTypeTo)
            {
                return rate.Rate;
            }

            return 1 / rate.Rate;
        }

        public async Task<decimal> ConvertCurrencyAsync(int idCurrencyTypeFrom, int idCurrencyTypeTo, decimal amount, DateTime date, decimal? rate)
        {
            if (!rate.HasValue)
            {
                rate = await GetCurrencyRateAsync(idCurrencyTypeFrom, idCurrencyTypeTo, date);
            }

            return decimal.Round(amount * rate.Value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
