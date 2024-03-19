using System;
using System.Data.Common;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.TimeCollector;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Dapper;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class CurrencyRepositoryTest
    {
        [Fact]
        public async Task Get_Currency_Rate_Should_Return_Current_Rate_When_Rate_Exists()
        {
            // Arrange
            var currencyRate = new CurrencyRate
            {
                IdCurrencyTypeFrom = 0,
                IdCurrencyTypeTo = 1,
                Rate = 106,
            };

            var idCurrencyTypeFrom = 0;
            var idCurrencyTypeTo = 1;
            var date = DateTime.UtcNow;

            // Arrange
            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<CurrencyRate>(null, null, null, null, null)).ReturnsAsync(currencyRate);

            var mockConnectionFactory = new Mock<IDatabaseConnectionFactory>();
            mockConnectionFactory.Setup(c => c.GetConnection()).Returns(mockConnection.Object);

            var repository = new CurrencyRepository(mockConnectionFactory.Object, Mock.Of<ITimeCollector>());

            //Act
            var result = await repository.GetCurrencyRateAsync(idCurrencyTypeFrom, idCurrencyTypeTo, date);

            // Assert
            Assert.Equal(currencyRate.Rate, result);
        }

        [Fact]
        public async Task Convert_Currency_Should_Return_The_Amount_Converted_Using_Rate_Store_In_Database_When_Rate_Is_Not_Passed()
        {
            // Arrange
            var currencyRate = new CurrencyRate
            {
                IdCurrencyTypeFrom = 0,
                IdCurrencyTypeTo = 1,
                Rate = 106,
            };

            var idCurrencyTypeFrom = 0;
            var idCurrencyTypeTo = 1;
            var date = DateTime.UtcNow;
            var amount = 100;

            // Arrange
            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<CurrencyRate>(null, null, null, null, null)).ReturnsAsync(currencyRate);

            var mockConnectionFactory = new Mock<IDatabaseConnectionFactory>();
            mockConnectionFactory.Setup(c => c.GetConnection()).Returns(mockConnection.Object);

            var repository = new CurrencyRepository(mockConnectionFactory.Object, Mock.Of<ITimeCollector>());

            //Act
            var result = await repository.ConvertCurrencyAsync(idCurrencyTypeFrom, idCurrencyTypeTo, amount, date, null);

            // Assert
            Assert.Equal(amount * currencyRate.Rate, result);
        }

        [Fact]
        public async Task Convert_Currency_Should_Return_The_Amount_Converted_Using_Rate_Passed_As_Parameter_When_Rate_Is_Passed()
        {
            // Arrange
            var currencyRate = new CurrencyRate
            {
                IdCurrencyTypeFrom = 0,
                IdCurrencyTypeTo = 1,
                Rate = 106,
            };

            var idCurrencyTypeFrom = 0;
            var idCurrencyTypeTo = 1;
            var date = DateTime.UtcNow;
            var amount = 100;
            var rate = 2;

            // Arrange
            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<CurrencyRate>(null, null, null, null, null)).ReturnsAsync(currencyRate);

            var mockConnectionFactory = new Mock<IDatabaseConnectionFactory>();
            mockConnectionFactory.Setup(c => c.GetConnection()).Returns(mockConnection.Object);

            var repository = new CurrencyRepository(mockConnectionFactory.Object, Mock.Of<ITimeCollector>());

            //Act
            var result = await repository.ConvertCurrencyAsync(idCurrencyTypeFrom, idCurrencyTypeTo, amount, date, rate);

            // Assert
            Assert.Equal(amount * rate, result);
        }
    }
}
