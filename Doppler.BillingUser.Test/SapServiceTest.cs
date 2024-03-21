using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.Sap;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Xunit;
using Doppler.BillingUser.TimeCollector;

namespace Doppler.BillingUser.Test
{
    public class SapServiceTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        [Fact]
        public async Task Send_user_to_sap_should_send_right_values()
        {
            // Arrange
            var businessPartner = new SapBusinessPartner
            {
                Id = 1,
                Email = "test1@example.com"
            };

            var service = new SapService(
                GetSapServiceSettingsMock().Object,
                Mock.Of<ILogger<SapService>>(),
                new PerBaseUrlFlurlClientFactory(),
                Mock.Of<IJwtTokenGenerator>(),
                Mock.Of<ITimeCollector>());
            using var httpTest = new HttpTest();

            //Act
            await service.SendUserDataToSap(businessPartner, It.IsAny<string>());
            const string url = "https://localhost:5000/businesspartner/createorupdatebusinesspartner";

            // Assert
            httpTest
                .ShouldHaveCalled(url)
                .WithVerb(HttpMethod.Post)
                .WithRequestBody("{\"Id\":1,\"IsClientManager\":false,\"FirstName\":null," +
                                "\"LastName\":null,\"BillingAddress\":null,\"CityName\":null,\"StateId\":null," +
                                "\"CountryCode\":null,\"Address\":null,\"ZipCode\":null,\"Email\":\"test1@example.com\"," +
                                "\"PhoneNumber\":null,\"FederalTaxId\":null,\"FederalTaxType\":\"CUIT\",\"IdConsumerType\":0," +
                                "\"GroupCode\":0,\"BillingEmails\":null,\"Cancelated\":false,\"SapProperties\":null," +
                                "\"Blocked\":false,\"IsInbound\":null,\"BillingZip\":null,\"BillingStateId\":null," +
                                "\"BillingCountryCode\":null,\"PaymentMethod\":0,\"PlanType\":null,\"BillingSystemId\":0," +
                                "\"ClientManagerType\":0,\"County\":null,\"BillingCity\":null}")
                .Times(1);
        }

        private static Mock<IOptions<SapSettings>> GetSapServiceSettingsMock()
        {
            var sapSettingsMock = new Mock<IOptions<SapSettings>>();
            sapSettingsMock.Setup(x => x.Value)
                .Returns(new SapSettings
                {
                    SapBaseUrl = "https://localhost:5000/",
                    SapCreateBusinessPartnerEndpoint = "businesspartner/createorupdatebusinesspartner"
                });

            return sapSettingsMock;
        }
    }
}
