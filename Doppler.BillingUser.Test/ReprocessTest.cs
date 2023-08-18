using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Moq;
using Doppler.BillingUser.ExternalServices.FirstData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Doppler.BillingUser.Encryption;

namespace Doppler.BillingUser.Test
{
    public class ReprocessTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJwcm92aXNvcnlfdW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsImlhdCI6MTY2ODEwODAxNiwiZXhwIjoxOTgzNzI3MjE2fQ.ej2ZvwjVks1B2CEwPGalEWBxIj995-W4CXNYpAuBS4_USJ2WKJX2tFG6BS7CkAznMYvIom1Unym6vFSaKdQTlP38VwvEohr9FZA5MiLolB1Owddl5Qbu7VXOAChyO14055LvMeJD3aj1HturtndhU_Qv6z9Q29L7Qk2cUN5SjeQgnn9lAxf0nHVG4y-3Q-9tX75EBT4Y5HVHfncvGat4b1K-K8bOC9eXFcdAALIHHsDwWxhOsx_0HMlfBJLc0ThaIiJ0zr2Z-h6jbbfsE6eUHloIZ85ptHl7inLW4j0Wi9jDsYHVjOdgEDwzKhf-U-Uc-3G2rvp9tvhSS0vbDdt0xw";
        private readonly WebApplicationFactory<Startup> _factory;
        public ReprocessTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PUT_Reprocess_should_authorize_provisory_user()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payments/reprocess")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Reprocess_should_return_forbidden_if_provisory_user_tries_to_access_resources_not_owned()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test2@example.com/payments/reprocess")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Reprocess_method_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payments/reprocess");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Reprocess_should_answer_status_400_if_user_has_no_invoices_declined()
        {
            // Arrange
            var accountName = "test1@example.com";

            var userId = 1;

            var authorizatioNumber = "LLLTD222";

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetInvoices(userId, PaymentStatusEnum.DeclinedPaymentTransaction, PaymentStatusEnum.ClientPaymentTransactionError, PaymentStatusEnum.FailedPaymentTransaction, PaymentStatusEnum.DoNotHonorPaymentResponse))
                .ReturnsAsync(new List<AccountingEntry>());

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName)).ReturnsAsync(new User()
            {
                IdUser = 1
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(authorizatioNumber);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payments/reprocess")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Reprocess_should_answer_status_200_if_reprocess_was_succesful()
        {
            // Arrange
            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var accountName = "test1@example.com";

            var userId = 1;

            var authorizatioNumber = "LLLTD222";

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.GetInvoices(userId, PaymentStatusEnum.DeclinedPaymentTransaction, PaymentStatusEnum.ClientPaymentTransactionError, PaymentStatusEnum.FailedPaymentTransaction, PaymentStatusEnum.DoNotHonorPaymentResponse))
                .ReturnsAsync(new List<AccountingEntry>()
                {
                    new AccountingEntry() { Amount = 1, Status = PaymentStatusEnum.DeclinedPaymentTransaction }
                });

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName)).ReturnsAsync(new User()
            {
                IdUser = 1
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(authorizatioNumber);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payments/reprocess")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
