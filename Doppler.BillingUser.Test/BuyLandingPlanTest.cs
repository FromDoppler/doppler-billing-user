using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.ExternalServices.Clover.Entities;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class BuyLandingPlanTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private readonly WebApplicationFactory<Startup> _factory;

        public BuyLandingPlanTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task POST_buy_landings_should_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var accountname = "test1@example.com";

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Post, $"accounts/{accountname}/landings/buy")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_buy_landings_should_return_not_found_when_user_not_exists()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(null as UserBillingInformation);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Invalid user", messageError);
        }

        [Fact]
        public async Task POST_buy_landings_should_return_bad_request_when_user_is_canceled()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = true
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("UserCanceled", messageError);
        }

        [Fact]
        public async Task POST_buy_landings_should_return_bad_request_when_payment_method_is_invalid()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = false,
                PaymentMethod = Enums.PaymentMethodEnum.NONE
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Invalid payment method", messageError);
        }

        [Fact]
        public async Task POST_buy_landings_should_return_bad_request_when_user_payment_type_is_transfer_and_billing_country_is_not_supported()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = false,
                PaymentMethod = Enums.PaymentMethodEnum.TRANSF,
                IdBillingCountry = 1
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Invalid payment method", messageError);
        }

        [Fact]
        public async Task POST_buy_landings_should_return_bad_request_when_user_has_not_marketing_plan()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = false,
                PaymentMethod = Enums.PaymentMethodEnum.TRANSF,
                IdBillingCountry = 10
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(null as BillingCredit);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Invalid marketing plan", messageError);
        }

        [Fact]
        public async Task POST_buy_landings_should_return_bad_request_when_user_has_marketing_plan_and_not_activated()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = false,
                PaymentMethod = Enums.PaymentMethodEnum.TRANSF,
                IdBillingCountry = 10
            };

            var currentBillingCredit = new BillingCredit
            {
                IdBillingCredit = 1,
                ActivationDate = null
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(currentBillingCredit);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Invalid marketing plan", messageError);
        }

        [Fact]
        public async Task POST_buy_landings_should_return_internal_server_error_when_missing_credit_card_information()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = false,
                PaymentMethod = Enums.PaymentMethodEnum.CC
            };

            var currentBillingCredit = new BillingCredit
            {
                IdBillingCredit = 1,
                ActivationDate = DateTime.Now
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(null as Doppler.BillingUser.ExternalServices.FirstData.CreditCard);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(currentBillingCredit);
            billingRepositoryMock.Setup(x => x.GetCurrentBillingCreditForLanding(It.IsAny<int>())).ReturnsAsync(null as BillingCredit);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var messageError = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("User credit card missing", messageError);
        }

        [Fact]
        public async Task POST_buy_landings_should_internal_server_error_when_first_data_payment_fails()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = false,
                PaymentMethod = Enums.PaymentMethodEnum.CC
            };

            var currentBillingCredit = new BillingCredit
            {
                IdBillingCredit = 1,
                ActivationDate = DateTime.Now
            };

            var creditCard = new BillingUser.ExternalServices.FirstData.CreditCard
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };


            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(currentBillingCredit);
            billingRepositoryMock.Setup(x => x.GetCurrentBillingCreditForLanding(It.IsAny<int>())).ReturnsAsync(null as BillingCredit);


            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception());

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);


            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_buy_landings_should_ok_when_information_is_correct()
        {
            // Arrange
            var accountname = "test1@example.com";

            var buyLandingPlans = new BuyLandingPlans
            {
                Total = 10,
                LandingPlans = new List<BuyLandingPlanItem>()
            };

            var user = new UserBillingInformation
            {
                IdUser = 1,
                IsCancelated = false,
                PaymentMethod = Enums.PaymentMethodEnum.CC
            };

            var currentBillingCredit = new BillingCredit
            {
                IdBillingCredit = 1,
                ActivationDate = DateTime.UtcNow,
                Date = DateTime.UtcNow,
                TotalMonthPlan = 3
            };

            var creditCard = new BillingUser.ExternalServices.FirstData.CreditCard
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };


            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(currentBillingCredit);
            billingRepositoryMock.Setup(x => x.GetCurrentBillingCreditForLanding(It.IsAny<int>())).ReturnsAsync(null as BillingCredit);
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(It.IsAny<BillingCreditAgreement>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync("1234");

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<ILandingPlanUserRepository>());
                    services.AddSingleton(Mock.Of<ISapService>());
                    services.AddSingleton(Mock.Of<IUserAddOnRepository>());
                    services.AddSingleton(Mock.Of<IEmailTemplatesService>());
                    services.AddSingleton(Mock.Of<ILandingPlanRepository>());
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);


            // Act
            var response = await client.PostAsync($"accounts/{accountname}/landings/buy", JsonContent.Create(buyLandingPlans));
            var message = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"Successful buy landing plans for: User: {accountname}", message);
        }
    }
}
