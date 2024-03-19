using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.TimeCollector;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class ZohoServiceTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        [Fact]
        public async Task Send_lead_update_to_zoho_when_request_body_has_all_values_ok()
        {
            // Arrange
            var leadId = "991102004120214390";
            var url = $"https://www.zohoapis.com/crm/v2/Leads/{leadId}";
            var body = "{\"data\":[{\"First_Name\":\"Nombre\",\"Last_Name\":\"Apellido\",\"Email\":\"test@example.com\",\"Lead_Source\":\"Admin\",\"Phone\":\"54 266 431-4463\",\"Country\":\"Argentina\",\"Doppler\":\"Individual\",\"D_Created_Date\":\"2021-08-19T15:36:51Z\",\"D_Status\":\"Active\",\"D_First_Payment\":\"2022-01-11T19:20:39Z\",\"D_Billing_System\":\"CC\",\"D_Origin\":\"login\",\"D_Origin_Cookies\":\"login\",\"D_Upgrade_Date\":\"2022-01-11T19:20:39Z\",\"D_UserId\":309929,\"D_Confirmation\":\"2021-08-19T15:37:16Z\",\"D_Last_Login_2\":\"2022-01-07T13:03:20Z\",\"D_Cant_Login\":5,\"D_Campa_as\":0,\"D_Creacion_lista\":\"No\",\"D_Integraciones\":\"No\",\"D_DKIM_SPF\":\"No\",\"Capsulas\":0,\"D_Domain_Score2\":0,\"D_Last_Price_Visit\":\"2021-08-19T15:40:08Z\",\"D_Cant_Visits_Prices\":2,\"UTM_Source\":\"direct\",\"UTM_Cookie1\":\"Date: 8/19/2021 3:35:52 PM +00:00, UTMSource: direct, UTMMedium: , UTMCampaign: , UTMTerm: , UTMContent:\",\"D_Primera_Base\":0,\"Lim500free\":\"No\",\"id\":\"119045987120124320\",\"owner\":{\"id\":\"198002440035064001\",\"name\":\"Nombre Apellido\",\"email\":\"ddonofrio@makingsense.com\"},\"Created_By\":{\"id\":\"198002440035064001\",\"name\":\"Nombre Apellido\",\"email\":\"ddonofrio@makingsense.com\"},\"Modified_By\":{\"id\":\"198002440035064001\",\"name\":\"Nombre Apellido\",\"email\":\"ddonofrio@makingsense.com\"},\"Created_Time\":\"2021-08-19T15:37:17Z\",\"Modified_Time\":\"2022-01-10T14:29:39Z\",\"Last_Activity_Time\":\"2022-01-10T14:29:39Z\"}]}";

            var service = new ZohoService(
                GetZohoServiceSettingsMock().Object,
                new PerBaseUrlFlurlClientFactory(),
                Mock.Of<ITimeCollector>());

            using var httpTest = new HttpTest();

            //Act
            await service.UpdateZohoEntityAsync(body, leadId, "Leads");

            // Assert
            httpTest
                .ShouldHaveCalled(url)
                .WithVerb(HttpMethod.Put)
                .WithRequestBody(body)
                .Times(1);
        }

        private static Mock<IOptions<ZohoSettings>> GetZohoServiceSettingsMock()
        {
            var zohoSettingsMock = new Mock<IOptions<ZohoSettings>>();
            zohoSettingsMock.Setup(x => x.Value)
                .Returns(new ZohoSettings
                {
                    UseZoho = true,
                    BaseUrl = "https://www.zohoapis.com/crm/v2/"
                });

            return zohoSettingsMock;
        }
    }
}
