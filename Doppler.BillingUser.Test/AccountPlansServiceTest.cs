using System.Net.Http;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.TimeCollector;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class AccountPlansServiceTest
    {
        [Fact]
        public async Task Get_account_plans_total_return_true_when_total_amount_is_equal_that_current_total_agreement()
        {
            // Arrange
            var agreement = new AgreementInformation
            {
                Total = 2,
                PlanId = 1,
                DiscountId = 3
            };
            var accountname = "test@example.com";

            var factory = new PerBaseUrlFlurlClientFactory();
            var service = new AccountPlansService(
                GetAccountPlansSettingsMock().Object,
                Mock.Of<ILogger<AccountPlansService>>(),
                //factory,
                Mock.Of<ICurrentRequestApiTokenGetter>(),
                Mock.Of<ITimeCollector>());

            // Act
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { total = 2 });

            var isValid = await service.IsValidTotal(accountname, agreement);
            const string url = "https://localhost:5000/accounts/test%40example.com/newplan/1/calculate?discountId=3";

            // Assert
            Assert.True(isValid);

            httpTest
                .ShouldHaveCalled(url)
                .WithVerb(HttpMethod.Get);
        }

        [Fact]
        public async Task Get_account_plans_total_return_false_when_total_amount_is_not_equal_that_current_total_agreement()
        {
            // Arrange
            var agreement = new AgreementInformation
            {
                Total = 2,
                PlanId = 1,
                DiscountId = 3
            };
            var accountname = "test@example.com";

            var factory = new PerBaseUrlFlurlClientFactory();
            var service = new AccountPlansService(
                GetAccountPlansSettingsMock().Object,
                Mock.Of<ILogger<AccountPlansService>>(),
                //factory,
                Mock.Of<ICurrentRequestApiTokenGetter>(),
                Mock.Of<ITimeCollector>());
            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { Total = 3 });

            // Act
            var isValid = await service.IsValidTotal(accountname, agreement);
            const string url = "https://localhost:5000/accounts/test%40example.com/newplan/1/calculate?discountId=3";

            // Assert
            Assert.False(isValid);

            httpTest
                .ShouldHaveCalled(url)
                .WithVerb(HttpMethod.Get)
                .Times(1);
        }

        [Fact]
        public async Task Get_account_plans_total_return_http_500_when_can_not_connect_with_account_plans_api()
        {
            // Arrange
            var agreement = new AgreementInformation
            {
                Total = 2,
                PlanId = 1,
                DiscountId = 3
            };
            var accountname = "test@example.com";

            var factory = new PerBaseUrlFlurlClientFactory();
            var service = new AccountPlansService(
                GetAccountPlansSettingsMock().Object,
                Mock.Of<ILogger<AccountPlansService>>(),
                //factory,
                Mock.Of<ICurrentRequestApiTokenGetter>(),
                Mock.Of<ITimeCollector>());


            // Act
            using var httpTest = new HttpTest();
            httpTest.RespondWith(status: 500);

            // Assert
            await Assert.ThrowsAsync<FlurlHttpException>(async () =>
                // Act
                await service.IsValidTotal(accountname, agreement));
        }

        private Mock<IOptions<AccountPlansSettings>> GetAccountPlansSettingsMock()
        {
            var accountPlansSettingsMock = new Mock<IOptions<AccountPlansSettings>>();
            accountPlansSettingsMock.Setup(x => x.Value)
                .Returns(new AccountPlansSettings
                {
                    CalculateUrlTemplate = "https://localhost:5000/accounts/{accountname}/newplan/{planId}/calculate?discountId={discountId}"
                });

            return accountPlansSettingsMock;
        }
    }
}
