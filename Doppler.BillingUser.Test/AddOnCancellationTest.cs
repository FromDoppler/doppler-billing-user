using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
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
    public class AddOnCancellationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
 
        private readonly WebApplicationFactory<Startup> _factory;

        public AddOnCancellationTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908, "onsite")]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908, "pushnotification")]
        [InlineData("", "onsite")]
        [InlineData("invalid", "pushnotification")]
        public async Task PUT_cancel_addon_plan_should_return_unauthorized_When_token_is_invalid(string token, string addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/addon/{addOnType}/cancel");
            request.Headers.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("onsite")]
        [InlineData("pushnotification")]
        public async Task PUT_cancel_addon_plan_should_return_not_found_when_user_not_have_any_plan(string addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";
            var expectedUser = new User() { IdUser = 1 };

            var userRepositoryMock = new Mock<IUserRepository>();
            var userAddOnRepositoryMock = new Mock<IUserAddOnRepository>();

            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(expectedUser);
            userAddOnRepositoryMock.Setup(x => x.GetByUserIdAndAddOnType(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserAddOn)null);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(userAddOnRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                });
            }).CreateClient();

            string planText = addOnType.ToLower() == "onsite" ? "onsite" : "push notification";

            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/addon/{addOnType}/cancel");
            request.Headers.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.SendAsync(request);
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal($"The user does not have any {planText} plan", messageError);

            int expectedAddOn = addOnType.ToLower() == "onsite" ? (int)AddOnType.OnSite : (int)AddOnType.PushNotification;
            userAddOnRepositoryMock.Verify(x => x.GetByUserIdAndAddOnType(expectedUser.IdUser, expectedAddOn), Times.Once());
        }

        [Theory]
        [InlineData("onsite")]
        [InlineData("pushnotification")]
        public async Task PUT_cancel_addon_plan_should_return_ok_when_information_is_correct(string addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";
            var expectedUser = new User() { IdUser = 1 };
            var expectedUserAddOn = new UserAddOn() { IdCurrentBillingCredit = 123 };

            string planText;
            int expectedAddOn;
            int billingCreditCanceledType;
            int initialBillingCreditType;

            if (addOnType.ToLower() == "onsite")
            {
                planText = "onsite";
                expectedAddOn = (int)AddOnType.OnSite;
                billingCreditCanceledType = (int)BillingCreditTypeEnum.OnSite_Canceled;
                initialBillingCreditType = (int)BillingCreditTypeEnum.OnSite_Buyed_CC;
            }
            else
            {
                planText = "push notification";
                expectedAddOn = (int)AddOnType.PushNotification;
                billingCreditCanceledType = (int)BillingCreditTypeEnum.PushNotification_Canceled;
                initialBillingCreditType = (int)BillingCreditTypeEnum.PushNotification_Buyed_CC;
            }

            var expectedBillingCredit = new BillingCredit() { IdBillingCredit = 123, IdBillingCreditType = initialBillingCreditType };

            var userRepositoryMock = new Mock<IUserRepository>();
            var userAddOnRepositoryMock = new Mock<IUserAddOnRepository>();
            var billingRepositoryMock = new Mock<IBillingRepository>();

            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(expectedUser);
            userAddOnRepositoryMock.Setup(x => x.GetByUserIdAndAddOnType(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(expectedUserAddOn);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(expectedBillingCredit.IdBillingCredit))
                .ReturnsAsync(expectedBillingCredit);
            billingRepositoryMock.Setup(x => x.UpdateBillingCreditType(It.IsAny<int>(), It.IsAny<int>()));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(userAddOnRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                });
            }).CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/addon/{addOnType}/cancel");
            request.Headers.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.SendAsync(request);
            var messageResult = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"REG - Successful cancel {planText} plan for: User: {accountname}", messageResult);

            userAddOnRepositoryMock.Verify(x => x.GetByUserIdAndAddOnType(expectedUser.IdUser, expectedAddOn), Times.Once());
            billingRepositoryMock.Verify(x => x.GetBillingCredit(expectedBillingCredit.IdBillingCredit), Times.Once());
            billingRepositoryMock.Verify(x => x.UpdateBillingCreditType(expectedUserAddOn.IdCurrentBillingCredit, billingCreditCanceledType), Times.Once());
        }

        [Theory]
        [InlineData("onsite")]
        [InlineData("pushnotification")]
        public async Task PUT_cancel_addon_plan_should_return_not_found_when_user_not_exists(string addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                });
            }).CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Put, $"accounts/{accountname}/addon/{addOnType}/cancel");
            request.Headers.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.SendAsync(request);
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("The user does not exist", messageError);
        }
    }
}
