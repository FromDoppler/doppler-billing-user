using Doppler.BillingUser.ApiModels;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Mappers.PaymentStatus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class PaymentStatusMapperTest
    {
        [Theory]
        [MemberData(nameof(AllPaymentStatusApiEnumValues))]
        public void MapFromPaymentStatusApiEnumToPaymentStatusEnum_should_return_a_valid_value(PaymentStatusApiEnum value)
        {
            var result = value.MapToPaymentStatusEnum();

            Assert.Contains(result, Enum.GetValues<PaymentStatusEnum>());
        }
        public static IEnumerable<object[]> AllPaymentStatusApiEnumValues =>
            Enum.GetValues<PaymentStatusApiEnum>().Select(x => new object[] { x });

        [Theory]
        [MemberData(nameof(AllPaymentStatusEnumValues))]
        public void MapFromPaymentStatusEnumToPaymentStatusApiEnum_should_return_a_valid_value(PaymentStatusEnum value)
        {
            var result = value.MapToPaymentStatusApiEnum();

            Assert.Contains(result, Enum.GetValues<PaymentStatusApiEnum>());
        }
        public static IEnumerable<object[]> AllPaymentStatusEnumValues =>
            Enum.GetValues<PaymentStatusEnum>().Select(x => new object[] { x });
    }
}
