using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class LandingCancelationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private readonly WebApplicationFactory<Startup> _factory;
        public LandingCancelationTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task PUT_cancel_landing_should_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var accountname = "test1@example.com";

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/landings/cancel")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_cancel_landing_should_return_not_found_when_user_not_exists()
        {
            // Arrange
            var accountname = "test1@example.com";

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(null as User);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/landings/cancel")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("The user does not exist", messageError);
        }


        [Fact]
        public async Task PUT_cancel_landing_should_return_not_found_when_user_not_have_any_plan()
        {
            // Arrange
            var accountname = "test1@example.com";
            var expectedUser = new User() { IdUser = 1 };

            var userRepositoryMock = new Mock<IUserRepository>();
            var userAddOnRepository = new Mock<IUserAddOnRepository>();

            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(expectedUser);

            userAddOnRepository.Setup(x => x.GetByUserIdAndAddOnType(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(null as UserAddOn);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(userAddOnRepository.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/landings/cancel")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("The user does not have any landing plan", messageError);

            userAddOnRepository.Verify(x => x.GetByUserIdAndAddOnType(
                expectedUser.IdUser,
                (int)AddOnType.Landing), Times.Once());
        }

        [Fact]
        public async Task PUT_cancel_landing_should_return_ok_when_information_is_correct()
        {
            // Arrange
            var publishedLandingAmount = 0;
            var accountname = "test1@example.com";
            var expectedUser = new User() { IdUser = 1 };
            var expectedUserAddOn = new UserAddOn() { IdCurrentBillingCredit = 123 };

            var userRepositoryMock = new Mock<IUserRepository>();
            var userAddOnRepository = new Mock<IUserAddOnRepository>();
            var billingRepository = new Mock<IBillingRepository>();
            var landingPlanUserRepository = new Mock<ILandingPlanUserRepository>();

            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(expectedUser);

            userAddOnRepository.Setup(x => x.GetByUserIdAndAddOnType(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(expectedUserAddOn);

            billingRepository.Setup(x => x.UpdateBillingCreditType(It.IsAny<int>(), It.IsAny<int>()));

            landingPlanUserRepository.Setup(x => x.CancelLandingPLanByBillingCreditId(It.IsAny<int>()));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(userAddOnRepository.Object);
                    services.AddSingleton(billingRepository.Object);
                    services.AddSingleton(landingPlanUserRepository.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/landings/cancel")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } }
            };

            // Act
            var response = await client.SendAsync(request);
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"Successful cancel landing plan for: User: {accountname}", messageError);

            userAddOnRepository.Verify(x => x.GetByUserIdAndAddOnType(
                expectedUser.IdUser,
                (int)AddOnType.Landing), Times.Once());

            billingRepository.Verify(x => x.UpdateBillingCreditType(
                expectedUserAddOn.IdCurrentBillingCredit,
                (int)BillingCreditTypeEnum.Landing_Canceled), Times.Once());

            landingPlanUserRepository.Verify(x => x.CancelLandingPLanByBillingCreditId(
                expectedUserAddOn.IdCurrentBillingCredit), Times.Once());
        }
    }
}
