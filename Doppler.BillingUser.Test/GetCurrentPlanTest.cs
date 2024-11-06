using System.Data.Common;
using System.Net;
using System.Threading.Tasks;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class GetCurrentPlanTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private readonly WebApplicationFactory<Startup> _factory;

        public GetCurrentPlanTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_current_plan_return_not_found_when_account_does_not_have_plan_associated()
        {
            // Arrange
            var mockConnection = new Mock<DbConnection>();
            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>()))
                .ReturnsAsync(null as CurrentPlan);
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.GetAsync("accounts/test1@example.com/plans/current");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_current_plan_return_ok_when_account_has_valid_current_plan()
        {
            // Arrange
            var mockConnection = new Mock<DbConnection>();
            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>()))
                .ReturnsAsync(new CurrentPlan
                {
                    IdPlan = 1
                });
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.GetAsync("accounts/test1@example.com/plans/current");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_current_plan_return_ok_when_account_has_data_valid_of_current_plan()
        {
            // Arrange
            var expectedContent = "{\"idPlan\":1,\"planSubscription\":0,\"planType\":\"individual\",\"remainingCredits\":0,\"emailQty\":10,\"subscribersQty\":null,\"conversationQty\":null,\"description\":null,\"fee\":0,\"paymentMethod\":0,\"printQty\":null}";
            var mockConnection = new Mock<DbConnection>();
            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>()))
                .ReturnsAsync(new CurrentPlan
                {
                    IdPlan = 1,
                    PlanType = "individual",
                    EmailQty = 10
                });
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.GetAsync("accounts/test1@example.com/plans/current");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expectedContent, responseContent);
        }
    }
}
