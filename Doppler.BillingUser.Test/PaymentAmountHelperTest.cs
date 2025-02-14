using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class PaymentAmountHelperTest
    {
        public static IEnumerable<object[]> ConvertCurrencyData =>
            new List<object[]>
            {
                new object[] {
                    0.0029850746268657m,
                    2680,
                    new PaymentAmountDetail {
                        CurrencyRate = 0.0029850746268657m,
                        Taxes = 562.80m,
                        Total = 9.68m
                    }
                },
                new object[] {
                    0.0079916886438104m,
                    2127.21m,
                    new PaymentAmountDetail {
                        CurrencyRate = 0.0079916886438104m,
                        Taxes = 446.7141m,
                        Total = 20.57m,
                    }
                },
                new object[] {
                    0.0039370078740157m,
                    2339.34m,
                    new PaymentAmountDetail {
                        CurrencyRate = 0.0039370078740157m,
                        Taxes = 491.2614m,
                        Total = 11.14m,
                    }
                },
            };

        [Theory, MemberData(nameof(ConvertCurrencyData))]
        public async Task Convert_currency_amount(decimal rate, decimal amountInArs, PaymentAmountDetail expectedDetail)
        {

            var currencyRepository = new Mock<ICurrencyRepository>();
            currencyRepository.Setup(x => x.GetCurrencyRateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(rate);

            var convertedCurrency = amountInArs * rate;
            currencyRepository.Setup(x => x.ConvertCurrencyAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<DateTime>(),
                It.IsAny<decimal>())).ReturnsAsync(convertedCurrency);

            var service = new PaymentAmounthelper(currencyRepository.Object);

            var paymentDetail = await service.ConvertCurrencyAmount(Enums.CurrencyTypeEnum.sARG, Enums.CurrencyTypeEnum.UsS, amountInArs);

            Assert.Equal(JsonSerializer.Serialize(expectedDetail), JsonSerializer.Serialize(paymentDetail));
        }
    }
}
