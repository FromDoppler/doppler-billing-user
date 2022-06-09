using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Mappers.PaymentStatus;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class MercadoPagoServiceTest
    {
        [Fact]
        public async void Get_payment_by_id_returns_payment_when_mercadopagoapi_response_is_successful()
        {
            // Arrange
            var accountname = "test@example.com";
            var paymentId = 1;
            var expectedUrl = $"http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/{paymentId}";

            var factory = new DefaultFlurlClientFactory();
            var service = new MercadoPagoService(
                GetMercadoPagoSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                factory,
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IPaymentStatusMapper>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MercadoPagoPayment
            {
                Id = 1,
                Status = Enums.MercadoPagoPaymentStatusEnum.Approved,
                StatusDetail = "Accredited",
            });

            // Act
            var payment = await service.GetPaymentById(paymentId, accountname);

            // Assert
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithVerb(HttpMethod.Get);
            Assert.IsType<MercadoPagoPayment>(payment);
        }

        [Fact]
        public async void Get_payment_by_id_throws_exception_when_mercadopagoapi_response_is_unsuccessful()
        {
            var paymentId = 1;
            var accountname = "test@example.com";
            var factory = new DefaultFlurlClientFactory();
            var service = new MercadoPagoService(
                GetMercadoPagoSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                factory,
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IPaymentStatusMapper>());

            // Act
            using var httpTest = new HttpTest();
            httpTest.RespondWith(status: 500);

            // Assert
            await Assert.ThrowsAsync<FlurlHttpException>(async () => await service.GetPaymentById(paymentId, accountname));
        }


        private Mock<IOptions<MercadoPagoSettings>> GetMercadoPagoSettingsMock()
        {
            var mercadoPagoSettingsMock = new Mock<IOptions<MercadoPagoSettings>>();
            mercadoPagoSettingsMock.Setup(x => x.Value)
                .Returns(new MercadoPagoSettings
                {
                    MercadoPagoApiUrlTemplate = "http://localhost:5000/doppler-mercadopago/accounts/{accountname}/payment/{id}"
                });

            return mercadoPagoSettingsMock;
        }
    }
}
