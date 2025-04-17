using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Moq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Doppler.BillingUser.Enums;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Services;
using System;
using Doppler.BillingUser.Mappers.AddOn;

namespace Doppler.BillingUser.Test
{
    public class ActivateAddOnTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private const AddOnType onSite = AddOnType.OnSite;
        private const AddOnType pushNotification = AddOnType.PushNotification;

        private readonly WebApplicationFactory<Startup> factory;

        public ActivateAddOnTest(WebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task POST_activate_addon__should_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var accountname = "test1@example.com";

            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"accounts/{accountname}/addon/OnSite/activate")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData(onSite)]
        [InlineData(pushNotification)]
        public async Task POST_activate_addon_plan_should_return_not_found_when_user_not_exists(AddOnType addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(null as User);

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/addon/{addOnType}/activate", null);
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("The user does not exist", messageError);
        }

        [Theory]
        [InlineData(onSite)]
        [InlineData(pushNotification)]
        public async Task POST_activate_onsite_plan_should_return_bad_request_when_have_an_active_addon_plan(AddOnType addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";
            var user = new User
            {
                IdUser = 1
            };

            var currentUserType = new UserTypePlanInformation
            {
                IdUserType = UserTypeEnum.MONTHLY
            };

            var userAddOn = new UserAddOn
            {

                IdUser = 1
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(currentUserType);

            var userAddOnRepositoryMock = new Mock<IUserAddOnRepository>();
            userAddOnRepositoryMock.Setup(x => x.GetByUserIdAndAddOnType(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(userAddOn);

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userAddOnRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/addon/{addOnType}/activate", null);
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal($"The user have an {addOnType} plan", messageError);
        }


        [Theory]
        [InlineData(onSite)]
        [InlineData(pushNotification)]
        public async Task POST_activate_addon_plan_should_return_ok_when_information_is_correct(AddOnType addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";
            var user = new User
            {
                IdUser = 1,
                IdClientManager = null
            };

            var currentUserType = new UserTypePlanInformation
            {
                IdUserType = UserTypeEnum.MONTHLY
            };

            var addOnFreePlan = new AddOnPlan
            {
                Description = "test",
                FreeDays = 90,
                PlanId = 1,
            };

            var userType = !user.IdClientManager.HasValue ? "REG" : "CM";

            var onSiteFreePlan = new OnSitePlan
            {
                PlanId = 1,
                FreeDays = 5
            };

            var pushNotificationFreePlan = new PushNotificationPlan
            {
                PlanId = 1,
                FreeDays = 5
            };

            var onSitePlanRepositoryMock = new Mock<IOnSitePlanRepository>();
            onSitePlanRepositoryMock.Setup(x => x.GetFreeOnSitePlan()).ReturnsAsync(onSiteFreePlan);

            var pushNotificationPlanRepositoryMock = new Mock<IPushNotificationPlanRepository>();
            pushNotificationPlanRepositoryMock.Setup(x => x.GetFreePlan()).ReturnsAsync(pushNotificationFreePlan);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(currentUserType);

            var userAddOnRepositoryMock = new Mock<IUserAddOnRepository>();
            userAddOnRepositoryMock.Setup(x => x.GetByUserIdAndAddOnType(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(null as UserAddOn);

            var addOnMapperMock = new Mock<IAddOnMapper>();
            addOnMapperMock.Setup(x => x.GetAddOnFreePlanAsync()).ReturnsAsync(addOnFreePlan);
            addOnMapperMock.Setup(x => x.CreateAddOnPlanUserAsync(It.IsAny<AddOnPlanUser>())).ReturnsAsync(1);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateOnSitePlanUserAsync(It.IsAny<OnSitePlanUser>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.CreatePushNotificationPlanUserAsync(It.IsAny<PushNotificationPlanUser>())).ReturnsAsync(1);

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userAddOnRepositoryMock.Object);
                    services.AddSingleton(addOnMapperMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(onSitePlanRepositoryMock.Object);
                    services.AddSingleton(pushNotificationPlanRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/addon/{addOnType}/activate", null);
            var message = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"{userType} - Successful active {addOnType} plan for: User: {accountname}", message);
        }

        [Theory]
        [InlineData(onSite)]
        [InlineData(pushNotification)]
        public async Task POST_activate_addon_plan_should_return_status_500_when_activation_failed(AddOnType addOnType)
        {
            // Arrange
            var accountname = "test1@example.com";
            var user = new User
            {
                IdUser = 1,
                IdClientManager = null
            };

            var currentUserType = new UserTypePlanInformation
            {
                IdUserType = UserTypeEnum.MONTHLY
            };

            var addOnFreePlan = new AddOnPlan
            {
                Description = "test",
                FreeDays = 90,
                PlanId = 1,
            };

            var userType = !user.IdClientManager.HasValue ? "REG" : "CM";

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(currentUserType);

            var userAddOnRepositoryMock = new Mock<IUserAddOnRepository>();
            userAddOnRepositoryMock.Setup(x => x.GetByUserIdAndAddOnType(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(null as UserAddOn);

            var addOnMapperMock = new Mock<IAddOnMapper>();
            addOnMapperMock.Setup(x => x.GetAddOnFreePlanAsync()).ReturnsAsync(addOnFreePlan);
            addOnMapperMock.Setup(x => x.CreateAddOnPlanUserAsync(It.IsAny<AddOnPlanUser>())).ReturnsAsync(1);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateOnSitePlanUserAsync(It.IsAny<OnSitePlanUser>())).Throws(new Exception());
            billingRepositoryMock.Setup(x => x.CreatePushNotificationPlanUserAsync(It.IsAny<PushNotificationPlanUser>())).Throws(new Exception());

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userAddOnRepositoryMock.Object);
                    services.AddSingleton(addOnMapperMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/addon/{addOnType}/activate", null);
            var message = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal($"{userType} - Failed at activating {addOnType} plan for: User: {accountname}", message);
        }
    }
}
