using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.Clover.Errors;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Services;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class MercadoPagoServiceTest
    {
        private readonly string _accountname;
        private readonly int _userId;
        private readonly int _paymentId;
        private readonly decimal _total;
        private readonly CreditCard _creditCard;
        private readonly PaymentRequestDto _paymentRequestDto;
        public MercadoPagoServiceTest()
        {
            _accountname = "test@example.com";
            _userId = 1;
            _paymentId = 1;
            _total = 100;

            _creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Mastercard,
                ExpirationMonth = 12,
                ExpirationYear = 2023,
                HolderName = "EncryptedHolderName",
                Number = "EncryptedNumber",
                Code = "EncryptedCode",
                IdentificationNumber = "11222333",
                IdentificationType = "DNI"
            };

            _paymentRequestDto = new PaymentRequestDto
            {
                TransactionAmount = 100,
                Installments = 1,
                TransactionDescription = "Doppler Email Marketing",
                Description = "MERPAGO*DOPPLER",
                PaymentMethodId = "master",
                Card = new CardDto
                {
                    Cardholder = new PaymentCardholder
                    {
                        Identification = new Identification
                        {
                            Number = "11222333",
                            Type = "DNI"
                        },
                        Name = "HolderName"
                    },
                    CardNumber = "Number",
                    SecurityCode = "Code",
                    ExpirationMonth = "12",
                    ExpirationYear = "2023"
                }
            };
        }

        [Fact]
        public async void Get_payment_by_id_returns_payment_when_mercadopagoapi_response_is_successful()
        {
            // Arrange
            var expectedUrl = $"http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/{_paymentId}";

            var service = new MercadoPagoService(
                GetMercadoPagoSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MercadoPagoPayment
            {
                Id = 1,
                Status = MercadoPagoPaymentStatusEnum.Approved,
                StatusDetail = "Accredited",
            });

            // Act
            var payment = await service.GetPaymentById(_paymentId, _accountname);

            // Assert
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithVerb(HttpMethod.Get);
            Assert.IsType<MercadoPagoPayment>(payment);
        }

        [Fact]
        public async void Get_payment_by_id_throws_exception_when_mercadopagoapi_response_is_unsuccessful()
        {
            // Arrange
            var service = new MercadoPagoService(
                GetMercadoPagoSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IEmailTemplatesService>());

            // Act
            using var httpTest = new HttpTest();
            httpTest.RespondWith(status: 500);

            // Assert
            await Assert.ThrowsAsync<FlurlHttpException>(async () => await service.GetPaymentById(_paymentId, _accountname));
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.In_Process)]
        public async void Create_payment_returns_payment_when_status_is_in_process_or_approved(MercadoPagoPaymentStatusEnum mercadoPagoPaymentStatus)
        {
            // Arrange
            var expectedUrl = "http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/";

            var service = new MercadoPagoService(
                GetMercadoPagoSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                GetEncryptionServiceMock().Object,
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MercadoPagoPayment
            {
                Status = mercadoPagoPaymentStatus
            });

            // Act
            var paymentCreated = await service.CreatePayment(_accountname, _userId, _total, _creditCard, true, false);

            // Assert
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithRequestJson(_paymentRequestDto)
                .WithVerb(HttpMethod.Post);
            Assert.IsType<MercadoPagoPayment>(paymentCreated);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatusEnum.Rejected)]
        [InlineData(MercadoPagoPaymentStatusEnum.Refunded)]
        [InlineData(MercadoPagoPaymentStatusEnum.Cancelled)]
        [InlineData(MercadoPagoPaymentStatusEnum.Charged_Back)]
        public async void Create_payment_throws_doppler_application_exception_with_error_code_DeclinedPaymentTransaction_when_payment_status_is_not_in_process_or_approved(MercadoPagoPaymentStatusEnum mercadoPagoPaymentStatus)
        {
            // Arrange
            var expectedUrl = "http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/";

            var service = new MercadoPagoService(
                GetMercadoPagoSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                GetEncryptionServiceMock().Object,
                Mock.Of<IEmailTemplatesService>());

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(new MercadoPagoPayment
            {
                Status = mercadoPagoPaymentStatus
            });

            // Act
            async Task CallFunc() => await service.CreatePayment(_accountname, _userId, _total, _creditCard, true, false);

            // Assert
            var caughtException = await Assert.ThrowsAsync<DopplerApplicationException>(CallFunc);
            Assert.Equal(PaymentErrorCode.DeclinedPaymentTransaction, caughtException.ErrorCode);
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithRequestJson(_paymentRequestDto)
                .WithVerb(HttpMethod.Post);
        }

        [Fact]
        public async void Create_payment_throws_doppler_application_exception_with_error_code_ClientPaymentTransactionError_when_mercadopagoapi_response_is_unsuccessful()
        {
            var expectedUrl = "http://localhost:5000/doppler-mercadopago/accounts/test%40example.com/payment/";

            var service = new MercadoPagoService(
                GetMercadoPagoSettingsMock().Object,
                Mock.Of<IJwtTokenGenerator>(),
                new DefaultFlurlClientFactory(),
                Mock.Of<ILogger<MercadoPagoService>>(),
                Mock.Of<IEncryptionService>(),
                Mock.Of<IEmailTemplatesService>());

            var apiError = new ApiError { Message = "Error" };

            using var httpTest = new HttpTest();
            httpTest.RespondWithJson(apiError, 500);

            // Act
            async Task CallFunc() => await service.CreatePayment(_accountname, _userId, _total, _creditCard, true, false);

            // Assert
            var caughtException = await Assert.ThrowsAsync<DopplerApplicationException>(CallFunc);
            Assert.Equal(PaymentErrorCode.ClientPaymentTransactionError, caughtException.ErrorCode);
            httpTest
                .ShouldHaveCalled(expectedUrl)
                .WithVerb(HttpMethod.Post);
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

        private Mock<IEncryptionService> GetEncryptionServiceMock()
        {
            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(es => es.DecryptAES256("EncryptedHolderName")).Returns("HolderName");
            encryptionServiceMock.Setup(es => es.DecryptAES256("EncryptedNumber")).Returns("Number");
            encryptionServiceMock.Setup(es => es.DecryptAES256("EncryptedCode")).Returns("Code");

            return encryptionServiceMock;
        }
    }
}
