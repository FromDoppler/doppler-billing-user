using Doppler.BillingUser.Controllers;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static System.Collections.Specialized.BitVector32;

namespace Doppler.BillingUser.Test
{
    public class PostMercadoPagoNotificationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly Mock<IBillingRepository> _billingRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMercadoPagoService> _mercadopagoServiceMock;
        private readonly Mock<ILogger<WebhooksController>> _loggerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IEncryptionService> _encryptionServiceMock;

        private readonly MercadoPagoNotification _notification;
        private readonly HttpClient _client;

        private readonly AccountingEntry _invoice;

        public PostMercadoPagoNotificationTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _billingRepositoryMock = new Mock<IBillingRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _mercadopagoServiceMock = new Mock<IMercadoPagoService>();
            _loggerMock = new Mock<ILogger<WebhooksController>>();
            _emailSenderMock = new Mock<IEmailSender>();
            _encryptionServiceMock = new Mock<IEncryptionService>();

            _notification = new MercadoPagoNotification
            {
                Action = "payment.updated",
                Data = new Data { Id = 1 },
            };

            _invoice = new AccountingEntry
            {
                Status = PaymentStatusEnum.Pending
            };

            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(_mercadopagoServiceMock.Object);
                    services.AddSingleton(_userRepositoryMock.Object);
                    services.AddSingleton(_billingRepositoryMock.Object);
                    services.AddSingleton(_loggerMock.Object);
                    services.AddSingleton(_emailSenderMock.Object);
                    services.AddSingleton(_encryptionServiceMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
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
        public async Task Update_mercadopago_payment_status_should_return_bad_request_when_payment_action_different_that_payment_updated(string action)
        {
            // Act
            var response = await _client.PostAsync("/accounts/test1@example.com/integration/mercadopagonotification", JsonContent.Create(new MercadoPagoNotification { Action = action }));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_should_return_not_found_when_user_not_exists()
        {
            // Arrange
            _userRepositoryMock.Setup(x => x.GetUserBillingInformation("test1@example.com")).ReturnsAsync((UserBillingInformation)null);

            // Act
            var response = await _client.PostAsync("/accounts/test1@example.com/integration/mercadopagonotification", JsonContent.Create(new MercadoPagoNotification { Action = "payment.updated" }));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PUpdate_mercadopago_payment_status_should_return_not_found_when_invoice_not_exists()
        {
            // Arrange
            _userRepositoryMock.Setup(x => x.GetUserBillingInformation("test1@example.com"))
                .ReturnsAsync((UserBillingInformation)new UserBillingInformation { IdUser = 5 });

            _billingRepositoryMock.Setup(x => x.GetPendingBillingCreditsAsync(1, PaymentMethodEnum.MP))
                .ReturnsAsync(new List<BillingCredit>());
            _billingRepositoryMock.Setup(x => x.GetInvoice(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((AccountingEntry)null);

            // Act
            var response = await _client.PostAsync("/accounts/test1@example.com/integration/mercadopagonotification", JsonContent.Create(_notification));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"Invoice with authorization number: {_notification.Data.Id} was not found.")), null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_should_add_LogError_when_billing_credits_does_not_exist()
        {
            // Arrange
            var accountName = "test1@example.com";

            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var invoice = new AccountingEntry
            {
                Status = PaymentStatusEnum.Pending
            };

            var mercadoPagoPayment = new MercadoPagoPayment
            {
                Status = MercadoPagoPaymentStatusEnum.Approved
            };

            _userRepositoryMock.Setup(x => x.GetUserBillingInformation("test1@example.com"))
                .ReturnsAsync((UserBillingInformation)new UserBillingInformation { IdUser = 5 });
            _userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);

            _billingRepositoryMock.Setup(x => x.GetPendingBillingCreditsAsync(It.IsAny<int>(), It.IsAny<PaymentMethodEnum>()))
                .ReturnsAsync(new List<BillingCredit>());
            _billingRepositoryMock.Setup(x => x.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatusEnum>(), It.IsAny<string>(), It.IsAny<string>()));
            _billingRepositoryMock.Setup(x => x.CreatePaymentEntryAsync(It.IsAny<int>(), It.IsAny<AccountingEntry>()));
            _billingRepositoryMock.Setup(x => x.GetInvoice(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(invoice);

            _emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            _encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            _mercadopagoServiceMock.Setup(x => x.GetPaymentById(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(mercadoPagoPayment);

            // Act
            var response = await _client.PostAsync("/accounts/test1@example.com/integration/mercadopagonotification", JsonContent.Create(_notification));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"The billing credits does not exist for the user {accountName}. You must be manually update.")),
                                        null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task Update_mercadopago_payment_status_should_add_LogError_when_billing_credits_has_more_one()
        {
            // Arrange
            var accountName = "test1@example.com";

            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var invoice = new AccountingEntry
            {
                Status = PaymentStatusEnum.Pending
            };

            var mercadoPagoPayment = new MercadoPagoPayment
            {
                Status = MercadoPagoPaymentStatusEnum.Approved
            };

            var billingCredits = new List<BillingCredit>
            {
                new BillingCredit{ IdBillingCredit = 1 },
                new BillingCredit{ IdBillingCredit = 2 }
            };

            _userRepositoryMock.Setup(x => x.GetUserBillingInformation("test1@example.com"))
                .ReturnsAsync((UserBillingInformation)new UserBillingInformation { IdUser = 5 });
            _userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);

            _billingRepositoryMock.Setup(x => x.GetPendingBillingCreditsAsync(It.IsAny<int>(), It.IsAny<PaymentMethodEnum>()))
                .ReturnsAsync(billingCredits);
            _billingRepositoryMock.Setup(x => x.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatusEnum>(), It.IsAny<string>(), It.IsAny<string>()));
            _billingRepositoryMock.Setup(x => x.CreatePaymentEntryAsync(It.IsAny<int>(), It.IsAny<AccountingEntry>()));
            _billingRepositoryMock.Setup(x => x.GetInvoice(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(invoice);

            _emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            _encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            _mercadopagoServiceMock.Setup(x => x.GetPaymentById(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(mercadoPagoPayment);

            // Act
            var response = await _client.PostAsync("/accounts/test1@example.com/integration/mercadopagonotification", JsonContent.Create(_notification));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"Can not update the billing credit because the user {accountName} has more one pending billing credits. You must be manually update.")),
                                        null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
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
        public async Task Update_mercadopago_payment_status_should_update_invoice_status_once_and_cancel_billing_credits_when_mp_payment_is_not_successful(MercadoPagoPaymentStatusEnum mpStatus, PaymentStatusEnum invoiceStatus)
        {
            // Arrange
            var invoice = new Model.AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };

            var billingCredits = new List<BillingCredit>
            {
                new BillingCredit
                {
                    IdBillingCredit = 1,
                    CreditsQty = 1000,
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL,
                    IdBillingCreditType = (int)BillingCreditTypeEnum.UpgradeRequest
                }
            };

            _userRepositoryMock.Setup(ur => ur.GetUserBillingInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.UserBillingInformation());

            _billingRepositoryMock.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);
            _billingRepositoryMock.Setup(x => x.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatusEnum>(), It.IsAny<string>(), It.IsAny<string>()));
            _billingRepositoryMock.Setup(x => x.GetPendingBillingCreditsAsync(It.IsAny<int>(), It.IsAny<PaymentMethodEnum>()))
                .ReturnsAsync(billingCredits);
            _billingRepositoryMock.Setup(x => x.GetPreviousBillingCreditNotCancelledByIdUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((BillingCredit)null);

            _mercadopagoServiceMock.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = mpStatus, StatusDetail = "cc_rejected_high_risk" });

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            _billingRepositoryMock.Verify(br => br.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatusEnum.DeclinedPaymentTransaction, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(MercadoPagoPaymentStatusEnum.Approved, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Approved, PaymentStatusEnum.DeclinedPaymentTransaction)]
        [InlineData(MercadoPagoPaymentStatusEnum.Authorized, PaymentStatusEnum.Pending)]
        [InlineData(MercadoPagoPaymentStatusEnum.Authorized, PaymentStatusEnum.DeclinedPaymentTransaction)]
        public async Task Update_mercadopago_payment_should_update_invoice_and_create_payment_when_payment_is_approved(MercadoPagoPaymentStatusEnum paymentStatus, PaymentStatusEnum invoiceStatus)
        {
            // Arrange
            var accountName = "test1@example.com";
            var partialBalance = 100;

            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var invoice = new AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };

            var billingCredits = new List<BillingCredit>
            {
                new BillingCredit
                {
                    IdBillingCredit = 1,
                    CreditsQty = 1000,
                    IdUserType = (int)UserTypeEnum.INDIVIDUAL,
                    IdBillingCreditType = (int)BillingCreditTypeEnum.UpgradeRequest
                }
            };

            var user = new User
            {
                IdUser = 5,
                Language = "es",
                FirstName = "Test"
            };

            _userRepositoryMock.Setup(x => x.GetUserBillingInformation("test1@example.com")).ReturnsAsync((UserBillingInformation)new UserBillingInformation { IdUser = 5 });
            _userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);
            _userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(partialBalance);
            _userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(user);

            _billingRepositoryMock.Setup(x => x.GetPendingBillingCreditsAsync(It.IsAny<int>(), It.IsAny<PaymentMethodEnum>()))
                .ReturnsAsync(billingCredits);
            _billingRepositoryMock.Setup(x => x.UpdateInvoiceStatus(It.IsAny<int>(), It.IsAny<PaymentStatusEnum>(), It.IsAny<string>(), It.IsAny<string>()));
            _billingRepositoryMock.Setup(x => x.CreatePaymentEntryAsync(It.IsAny<int>(), It.IsAny<AccountingEntry>()));
            _billingRepositoryMock.Setup(x => x.GetInvoice(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(invoice);
            _billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>(), It.IsAny<int>())).ReturnsAsync(1);


            _mercadopagoServiceMock.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = paymentStatus });

            _emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            _encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            // Act
            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _loggerMock.Verify(m => m.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"Successful at creating movement credits for the User: {accountName}")),
                                        null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _loggerMock.Verify(m => m.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"Successful at creating payment history for the User: {accountName}")),
                                        null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _loggerMock.Verify(m => m.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"Successful at updating user billing credit for the User: {accountName}")),
                                        null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _loggerMock.Verify(m => m.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"Successful at approving the billing credit for: User: {accountName} and BillingCredit: {billingCredits.FirstOrDefault().IdBillingCredit}")),
                                        null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _loggerMock.Verify(m => m.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, _) => v.ToString().Contains($"Successful at updating the payment for: User: {accountName} - Billing Credit: {billingCredits.FirstOrDefault().IdBillingCredit}")),
                                        null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
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
        public async Task Update_mercadopago_payment_should_does_not_updates_invoice_status_or_create_payment_when_mp_payment_and_invoice_are_approved(MercadoPagoPaymentStatusEnum paymentStatus, PaymentStatusEnum invoiceStatus)
        {
            var invoice = new AccountingEntry
            {
                IdAccountingEntry = 1,
                Status = invoiceStatus,
            };

            _userRepositoryMock.Setup(ur => ur.GetUserBillingInformation(It.IsAny<string>()))
                .ReturnsAsync(new Model.UserBillingInformation());

            _billingRepositoryMock.Setup(br => br.GetInvoice(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(invoice);

            _mercadopagoServiceMock.Setup(mps => mps.GetPaymentById(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new MercadoPagoPayment { Status = paymentStatus });

            var response = await _client.PostAsJsonAsync($"accounts/test1@example.com/integration/mercadopagonotification", _notification);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
