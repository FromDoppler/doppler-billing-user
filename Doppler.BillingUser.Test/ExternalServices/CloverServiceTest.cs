using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.Clover;
using Doppler.BillingUser.ExternalServices.Clover.Errors;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Services;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test.ExternalServices
{
    public class CloverServiceTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        [Fact]
        public async Task Call_clove_api_should_be_return_true_when_credit_card_is_valid()
        {
            // Arrange
            var accountname = "test1@example.com";
            var clientId = 1000;
            var isFree = false;
            var creditCard = new CreditCard
            {
                ExpirationMonth = 1,
                ExpirationYear = 2025,
                HolderName = "Holder Test",
                Number = "4111111111111111",
                CardType = Enums.CardTypeEnum.Visa,
                Code = "123"
            };

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var service = new CloverService(
                GetCloverSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                new PerBaseUrlFlurlClientFactory(),
                Mock.Of<ILogger<CloverService>>(),
                encryptionServiceMock.Object,
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(true, 200);

            //Act
            await service.IsValidCreditCard(accountname, creditCard, clientId, isFree);
            string url = $"https://localhost:5000/accounts/{accountname}/creditcard/validate";

            // Assert
            httpTest
                .ShouldHaveCalled(url)
                .WithVerb(HttpMethod.Post)
                .WithRequestBody("{\"ChargeTotal\":0.0,\"CreditCard\":{\"CardNumber\":\"12345\",\"CardExpMonth\":\"1\",\"CardExpYear\":\"2025\",\"SecurityCode\":\"12345\",\"CardType\":\"VISA\",\"CardHolderName\":\"12345\"},\"ClientId\":\"1000\"}")
                .Times(1);
        }

        [Fact]
        public async Task Call_clover_api_should_be_return_flur_exception_when_credit_card_is_invalid()
        {
            // Arrange
            var accountname = "test1@example.com";
            var clientId = 1000;
            var isFree = false;
            var creditCard = new CreditCard
            {
                ExpirationMonth = 1,
                ExpirationYear = 2025,
                HolderName = "Holder Test",
                Number = "4111111111111111",
                CardType = Enums.CardTypeEnum.Visa,
                Code = "123"
            };

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var service = new CloverService(
                GetCloverSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                new PerBaseUrlFlurlClientFactory(),
                Mock.Of<ILogger<CloverService>>(),
                encryptionServiceMock.Object,
                Mock.Of<IEmailTemplatesService>());

            var apiError = new ApiError { Message = "Error", Error = new ApiErrorCause { Code = "Error", Message = "Error message" } };

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(apiError, 500);

            //Act
            Task act() => service.IsValidCreditCard(accountname, creditCard, clientId, isFree);

            // Assert
            var exception = await Assert.ThrowsAsync<DopplerApplicationException>(act);
            Assert.Equal($"ClientPaymentTransactionError - {apiError.Message}", exception.Message);
        }

        private static Mock<IOptions<CloverSettings>> GetCloverSettingsMock()
        {
            var cloverSettingsMock = new Mock<IOptions<CloverSettings>>();
            cloverSettingsMock.Setup(x => x.Value)
                .Returns(new CloverSettings
                {
                    UseCloverApi = true,
                    BaseUrl = "https://localhost:5000"
                });

            return cloverSettingsMock;
        }
    }
}
