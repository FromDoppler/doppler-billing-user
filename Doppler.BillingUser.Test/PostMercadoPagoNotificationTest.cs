using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class PostMercadoPagoNotificationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly Mock<IBillingRepository> _billingRepository;
        private readonly Mock<IUserRepository> _userRepository;
        private readonly Mock<IMercadoPagoService> _mercadopagoService;
        private readonly MercadoPagoNotification _notification;
        private readonly HttpClient _client;

        public PostMercadoPagoNotificationTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _billingRepository = new Mock<IBillingRepository>();
            _userRepository = new Mock<IUserRepository>();
            _mercadopagoService = new Mock<IMercadoPagoService>();

            _notification = new MercadoPagoNotification
            {
                Action = "payment.updated",
                Data = new Data { Id = 1 },
            };
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(_mercadopagoService.Object);
                    services.AddSingleton(_userRepository.Object);
                    services.AddSingleton(_billingRepository.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_returns_internal_server_error_when_payment_not_found()
        {
            // Arrange
            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Model.AccountingEntry());
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_returns_notfound_when_user_is_null()
        {
            // Arrange
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Model.AccountingEntry());

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_returns_notfound_when_invoice_is_null()
        {
            // Arrange
            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatusEnum.Rejected, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Rejected, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.Refunded, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.Refunded, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Charged_Back, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.Charged_Back, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Cancelled, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.Cancelled, PaymentStatusEnum.Pending)]
        public async Task Update_mercadopago_payment_status_calls_update_invoice_status_once_when_mp_payment_is_not_successful(MercadoPagoPaymentStatusEnum mpStatus, PaymentStatusEnum invoiceStatus)
        {
            // Arrange
            var invoice = new Model.AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };

            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = mpStatus });

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatusEnum.DeclinedPaymentTransaction, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatusEnum.Approved, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Approved, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.Authorized, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Authorized, PaymentStatusEnum.DeclinedPaymentTransaction)]
        public async Task Update_mercadopago_payment_calls_update_invoice_and_create_payment_when_payment_is_approved(MercadoPagoPaymentStatusEnum paymentStatus, PaymentStatusEnum invoiceStatus)
        {
            // Arrange
            var invoice = new AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };

            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _userRepository.Setup(ur => ur.GetEncryptedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(new ExternalServices.FirstData.CreditCard());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = paymentStatus });

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatusEnum.Approved, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _billingRepository.Verify(br => br.CreatePaymentEntryAsync(invoice.IdAccountingEntry, It.IsAny<AccountingEntry>()), Times.Once);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatusEnum.Approved, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.Pending, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.Pending, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Pending, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.Authorized, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.In_Process, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.In_Process, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.In_Process, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.In_Mediation, PaymentStatusEnum.Approved)]
        [InlineData(MercadoPagoPaymentStatusEnum.In_Mediation, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.In_Mediation, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.Rejected, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.Cancelled, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.Refunded, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.Charged_Back, PaymentStatusEnum.DeclinedPaymentTransaction)]
        public async Task Update_mercadopago_payment_does_not_updates_invoice_status_or_create_payment_when_mp_payment_and_invoice_are_approved(MercadoPagoPaymentStatusEnum paymentStatus, PaymentStatusEnum invoiceStatus)
        {
            var invoice = new AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };
            _userRepository.Setup(ur => ur.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.User());
            _billingRepository.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);
            _mercadopagoService.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = paymentStatus });

            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatusEnum>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _billingRepository.Verify(br => br.CreatePaymentEntryAsync(It.IsAny<int>(), It.IsAny<AccountingEntry>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("payment.created")]
        [InlineData("application.deauthorized")]
        [InlineData("application.authorized")]
        [InlineData("subscription_preapproval.created")]
        [InlineData("updated")]
        [InlineData("state_FINISHED")]
        [InlineData("state_CANCELED")]
        [InlineData("state_ERROR")]
        [InlineData("shipment.updated")]

        public async Task Update_mercadopago_payment_does_not_call_any_method_when_notification_action_is_not_payment_updated(string action)
        {
            // Arrange
            _notification.Action = action;
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _userRepository.Verify(ur => ur.GetUserInformation(It.IsAny<string>()), Times.Never);
            _billingRepository.Verify(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _mercadopagoService.Verify(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
            _billingRepository.Verify(br => br.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatusEnum>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _billingRepository.Verify(br => br.CreatePaymentEntryAsync(It.IsAny<int>(), It.IsAny<AccountingEntry>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

    }
}
