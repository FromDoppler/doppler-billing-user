using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System;
using Flurl.Http.Testing;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Slack;
using Microsoft.Extensions.Options;
using Doppler.BillingUser.ExternalServices.EmailSender;
using System.Collections.Generic;
using System.Threading;
using Doppler.BillingUser.ExternalServices.Zoho;
using Microsoft.Extensions.Logging;
using Doppler.BillingUser.ExternalServices.Zoho.API;
using Doppler.BillingUser.ExternalServices.Clover;
using Doppler.BillingUser.ExternalServices.PaymentsApi;

namespace Doppler.BillingUser.Test
{
    public class PostAgreementTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private readonly WebApplicationFactory<Startup> _factory;

        public PostAgreementTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Create_agreement_return_ForbidResult_when_total_amount_validation_is_false()
        {
            // Arrange
            var agreement = new
            {
                Total = 10,
                DiscountId = 2,
                PlanId = 3
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(false);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(Mock.Of<IChatPlanUserRepository>());
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_Ok_when_total_amount_validation_is_true_and_planId_is_defined()
        {
            // Arrange
            var agreement = new
            {
                Total = 10,
                PlanId = 2
            };

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

            var authorizatioNumber = "LLLTD222";
            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task POST_agreement_should_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Post, "accounts/test1@example.com/agreements")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Post, "accounts/test1@example.com/agreements");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_bad_request_when_body_is_empty()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation("test1@example.com"))
                .ReturnsAsync(new UserBillingInformation() { Email = "test1@example.com", PaymentMethod = PaymentMethodEnum.CC });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());


            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(new { }));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_ok_when_planId_is_a_valid_prepaid_plan_and_user_exists_and_have_cc_as_payment_method_and_total_is_not_empty_and_has_valid_cc_and_first_datapayment_is_made_and_billing_credit_is_created_and_send_zoho_update()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var authorizatioNumber = "LLLTD222";
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(It.IsAny<BillingCreditAgreement>())).ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>(), null)).ReturnsAsync(movementCreditId);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var zohoServiceMock = new Mock<IZohoService>();
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", It.IsAny<string>()));
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", It.IsAny<string>()))
                .ReturnsAsync(
                new ZohoResponse<ZohoEntityLead>()
                {
                    Data = new List<ZohoEntityLead>()
                    {
                        new ZohoEntityLead()
                    }
                });

            zohoServiceMock.Setup(x => x.UpdateZohoEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ZohoUpdateResponse()
                {
                    Data = new List<ZohoUpdateResponseItem>()
                }
            );

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(GetZohoServiceSettingsMock().Object);
                    services.AddSingleton(zohoServiceMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_not_found_when_user_not_exists()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(null as UserBillingInformation);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_payment_method_is_not_cc()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>()))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.MP,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_is_not_a_free_user()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName)).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_total_is_not_present_in_payload()
        {
            // Arrange
            var planId = 1;

            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var accountName = "test1@example.com";
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName)).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(new { planId }));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_ok_when_total_is_zero()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 0
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(It.IsAny<BillingCreditAgreement>())).ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>(), null)).ReturnsAsync(movementCreditId);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_credit_card_not_exists()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(null as CreditCard);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_internal_server_error_when_user_first_data_payment_fails()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
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

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ThrowsAsync(new Exception());

            var accountServiceMock = new Mock<IAccountPlansService>();
            accountServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(accountServiceMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_internal_server_error_when_store_payment_records_on_db_fails()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
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

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ThrowsAsync(new Exception());

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(0);

            var accountServiceMock = new Mock<IAccountPlansService>();
            accountServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(accountServiceMock.Object);
                    services.AddSingleton(Mock.Of<ISlackService>());
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_new_plan_type_is_free()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName)).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.FREE
            });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_new_plan_type_is_not_valid()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_ok_when_total_is_zero_and_accounting_records_are_not_created_in_db()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 0
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(It.IsAny<BillingCreditAgreement>())).ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>(), null)).ReturnsAsync(movementCreditId);

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            billingRepositoryMock.Verify(ms => ms.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_ok_when_promocode_is_null()
        {
            // Arrange
            const string accountName = "test1@example.com";
            var agreement = new
            {
                planId = 1,
                total = 2
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(new CreditCard());
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(
                    It.IsAny<decimal>(),
                    It.IsAny<CreditCard>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .ReturnsAsync("authorizatioNumber");

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);
            var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { Total = 2 });

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_internal_server_error_when_promocode_is_invalid()
        {
            // Arrange
            const string accountName = "test1@example.com";
            var agreement = new
            {
                planId = 1,
                total = 2,
                promocode = "promocode-test"
            };
            var billingRepositoryMock = new Mock<IBillingRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);
            var httpTest = new HttpTest();
            httpTest.RespondWithJson(new { Total = 2 });
            httpTest.RespondWith(status: 500);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_ok_when_promocode_is_valid()
        {
            // Arrange
            const string accountName = "test1@example.com";
            var agreement = new
            {
                planId = 1,
                total = 0,
                promocode = "promocode-test"
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);

            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode("promocode-test", 1))
                .ReturnsAsync(new Promotion
                {
                    IdPromotion = 1
                });
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IPromotionRepository>());
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_increment_times_to_use_of_promocode_when_agreement_is_accomplished()
        {
            // Arrange
            const string accountName = "test1@example.com";
            var agreement = new
            {
                planId = 1,
                total = 0,
                promocode = "promocode-test"
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);

            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode("promocode-test", 1))
                .ReturnsAsync(new Promotion
                {
                    IdPromotion = 1
                });
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.INDIVIDUAL
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var promotionRepositoryMock = new Mock<IPromotionRepository>();
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(promotionRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            promotionRepositoryMock.Verify(x => x.IncrementUsedTimes(It.IsAny<Promotion>()), Times.Once());
        }

        [Fact]
        public async Task POST_agreement_information_should_notify_in_slack_when_first_data_payment_fails()
        {
            // Arrange
            var user = new UserBillingInformation
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var creditCard = new CreditCard
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>()))
                .ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>()))
                .ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception());

            var accountServiceMock = new Mock<IAccountPlansService>();
            accountServiceMock.Setup(x => x.IsValidTotal(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(accountServiceMock.Object);
                    services.AddSingleton(GetSlackSettingsMock().Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);
            var httpTest = new HttpTest();

            // Act
            var response = await client.PostAsJsonAsync("accounts/test1@example.com/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            httpTest.ShouldHaveCalled("https://hooks.slack.com/services/test")
                .WithVerb(HttpMethod.Post);
        }

        [Fact]
        public async Task POST_agreement_information_should_notify_to_slack_when_zoho_update_fails()
        {
            // Arrange
            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var creditCard = new CreditCard()
            {
                CardType = CardTypeEnum.Visa,
                ExpirationMonth = 12,
                ExpirationYear = 23,
                HolderName = "kBvAJf5f3AIp8+MEVYVTGA==",
                Number = "Oe9VdYnmPsZGPKnLEogk1hbP7NH3YfZnqxLrUJxnGgc=",
                Code = "pNw3zrff06X9K972Ro6OwQ=="
            };

            var authorizatioNumber = "LLLTD222";
            var invoiceId = 1;
            var billingCreditId = 1;
            var movementCreditId = 1;

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.UpdateUserBillingCredit(It.IsAny<UserBillingInformation>())).ReturnsAsync(1);
            userRepositoryMock.Setup(x => x.GetAvailableCredit(It.IsAny<int>())).ReturnsAsync(10);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(invoiceId);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(It.IsAny<BillingCreditAgreement>())).ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.CreateMovementCreditAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UserBillingInformation>(), It.IsAny<UserTypePlanInformation>(), null)).ReturnsAsync(movementCreditId);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var zohoServiceMock = new Mock<IZohoService>();
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", It.IsAny<string>()));
            zohoServiceMock.Setup(x => x.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", It.IsAny<string>()))
                .ReturnsAsync(
                new ZohoResponse<ZohoEntityLead>()
                {
                    Data = new List<ZohoEntityLead>()
                    {
                        new ZohoEntityLead()
                    }
                });

            zohoServiceMock.Setup(x => x.UpdateZohoEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(GetSlackSettingsMock().Object);
                    services.AddSingleton(GetZohoServiceSettingsMock().Object);
                    services.AddSingleton(zohoServiceMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });
            });

            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);
            var httpTest = new HttpTest();

            // Act
            var response = await client.PostAsync($"accounts/{accountName}/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            httpTest.ShouldHaveCalled("https://hooks.slack.com/services/test")
                .WithVerb(HttpMethod.Post);
        }

        [Fact]
        public async Task POST_agreement_should_return_Ok_and_send_three_notifications_when_user_type_is_subscriber()
        {
            // Arrange
            var agreement = new
            {
                Total = 10,
                PlanId = 2,
                DiscountId = 1
            };

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

            var authorizatioNumber = "LLLTD222";
            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.SUBSCRIBERS
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            billingRepositoryMock.Setup(x => x.GetPlanDiscountInformation(It.IsAny<int>())).ReturnsAsync(new PlanDiscountInformation()
            {
                DiscountPlanFee = 1,
                MonthPlan = 1
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            emailSenderMock.Verify(x => x.SafeSendWithTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<Attachment>>(),
                It.IsAny<CancellationToken>()),
                Times.Exactly(3));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_is_cancelated()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IsCancelated = true,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_Ok_when_user_payment_type_is_transfer()
        {
            // Arrange
            var agreement = new
            {
                Total = 10,
                PlanId = 2,
                DiscountId = 1
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.TRANSF,
                    IdBillingCountry = (int)CountryEnum.Colombia
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.SUBSCRIBERS
            });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            billingRepositoryMock.Setup(x => x.GetPlanDiscountInformation(It.IsAny<int>())).ReturnsAsync(new PlanDiscountInformation()
            {
                DiscountPlanFee = 1,
                MonthPlan = 1
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_payment_type_is_transfer_and_billing_country_is_not_supported()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.TRANSF,
                IdBillingCountry = (int)CountryEnum.Argentina
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(null as UserTypePlanInformation);
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_activate_standby_users_when_free_user_upgrades_plan()
        {
            var agreement = new
            {
                Total = 10,
                PlanId = 2
            };

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

            var authorizatioNumber = "LLLTD222";
            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(It.IsAny<string>())).ReturnsAsync(creditCard);
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(authorizatioNumber);

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            //Assert
            billingRepositoryMock.Verify(brm => brm.ActivateStandBySubscribers(It.IsAny<int>()), Times.Once);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_not_activate_standby_users_when_free_user_upgrades_plan_and_payment_is_pending()
        {
            // Arrange
            var agreement = new
            {
                Total = 10,
                PlanId = 2,
                DiscountId = 1
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new Promotion());
            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation()
                {
                    IdUser = 1,
                    PaymentMethod = PaymentMethodEnum.TRANSF,
                    IdBillingCountry = (int)CountryEnum.Argentina
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL
            });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>())).ReturnsAsync(new BillingCredit()
            {
                IdBillingCredit = 1,
                Date = new DateTime(2021, 12, 10)
            });

            billingRepositoryMock.Setup(x => x.GetPlanDiscountInformation(It.IsAny<int>())).ReturnsAsync(new PlanDiscountInformation()
            {
                DiscountPlanFee = 1,
                MonthPlan = 1
            });

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            //Assert
            billingRepositoryMock.Verify(brm => brm.ActivateStandBySubscribers(It.IsAny<int>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_information_should_return_bad_request_when_user_do_an_update_and_is_not_a_monthly_or_contacts()
        {
            // Arrange
            var user = new UserBillingInformation()
            {
                IdUser = 1,
                PaymentMethod = PaymentMethodEnum.CC,
                IdBillingCountry = 1
            };

            var agreement = new
            {
                planId = 1,
                total = 15
            };

            var accountName = "test1@example.com";
            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName)).ReturnsAsync(user);
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>())).ReturnsAsync(new UserTypePlanInformation()
            {
                IdUserType = UserTypeEnum.INDIVIDUAL,
                SubscribersQty = 1500,
                Fee = 25
            });
            userRepositoryMock.Setup(x => x.GetUserInformation(accountName))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    IdClientManager = null,
                    IdBillingCountry = 1
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsync("accounts/test1@example.com/agreements", JsonContent.Create(agreement));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_ok_when_update_plans_is_by_emails_or_contacts()
        {
            // Arrange
            const string accountName = "test1@example.com";
            var agreement = new
            {
                planId = 1,
                total = 15
            };

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

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>()))
                .ReturnsAsync(new BillingCredit
                {
                    Date = DateTime.UtcNow
                });

            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode("promocode-test", 1))
                .ReturnsAsync(new Promotion
                {
                    IdPromotion = 1
                });
            accountPlansServiceMock.Setup(x => x.GetCalculateUpgrade(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(new PlanAmountDetails
                {
                    Total = 100,
                    CurrentMonthTotal = 100,
                    DiscountPaymentAlreadyPaid = 0,
                    DiscountPlanFeeAdmin = new DiscountPlanFeeAdmin(),
                    DiscountPrepayment = new DiscountPrepayment(),
                    DiscountPromocode = new DiscountPromocode(),
                    NextMonthTotal = 150
                });

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    IdCurrentBillingCredit = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.MONTHLY,
                    IdUserTypePlan = 1,
                    EmailQty = 1500
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.MONTHLY,
                    IdUserTypePlan = 2,
                    EmailQty = 500
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync("ET123456");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(Mock.Of<IPromotionRepository>());
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<ISapService>());
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_agreement_should_return_ok_when_update_plans_is_by_emails_or_contacts_and_use_Clover()
        {
            // Arrange
            const string accountName = "test1@example.com";
            var agreement = new
            {
                planId = 1,
                total = 15
            };

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

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock.Setup(x => x.CreateAccountingEntriesAsync(It.IsAny<AccountingEntry>(), It.IsAny<AccountingEntry>())).ReturnsAsync(1);
            billingRepositoryMock.Setup(x => x.GetBillingCredit(It.IsAny<int>()))
                .ReturnsAsync(new BillingCredit
                {
                    Date = DateTime.UtcNow
                });

            var accountPlansServiceMock = new Mock<IAccountPlansService>();
            accountPlansServiceMock.Setup(x => x.IsValidTotal(accountName, It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);
            accountPlansServiceMock.Setup(x => x.GetValidPromotionByCode("promocode-test", 1))
                .ReturnsAsync(new Promotion
                {
                    IdPromotion = 1
                });
            accountPlansServiceMock.Setup(x => x.GetCalculateUpgrade(It.IsAny<string>(), It.IsAny<AgreementInformation>()))
                .ReturnsAsync(new PlanAmountDetails
                {
                    Total = 100,
                    CurrentMonthTotal = 100,
                    DiscountPaymentAlreadyPaid = 0,
                    DiscountPlanFeeAdmin = new DiscountPlanFeeAdmin(),
                    DiscountPrepayment = new DiscountPrepayment(),
                    DiscountPromocode = new DiscountPromocode(),
                    NextMonthTotal = 150
                });

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(accountName))
                .ReturnsAsync(new UserBillingInformation
                {
                    IdUser = 1,
                    IdCurrentBillingCredit = 1,
                    PaymentMethod = PaymentMethodEnum.CC,
                    IdBillingCountry = 1
                });
            userRepositoryMock.Setup(x => x.GetUserNewTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.MONTHLY,
                    IdUserTypePlan = 1,
                    EmailQty = 1500
                });
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>())).ReturnsAsync(new User()
            {
                Language = "es"
            });
            userRepositoryMock.Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>()))
                .ReturnsAsync(new UserTypePlanInformation
                {
                    IdUserType = UserTypeEnum.MONTHLY,
                    IdUserTypePlan = 2,
                    EmailQty = 500
                });
            userRepositoryMock.Setup(x => x.GetEncryptedCreditCard(accountName)).ReturnsAsync(creditCard);

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock.Setup(x => x.SafeSendWithTemplateAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Attachment>>(), It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var cloverServiceMock = new Mock<ICloverService>();
            cloverServiceMock.Setup(x => x.CreateCreditCardPayment(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync("ET123456");

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var paymentServiceMock = new Mock<IPaymentsService>();
            paymentServiceMock.Setup(x => x.Purchase(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<BillingUser.ExternalServices.FirstData.CreditCard>(),
                It.IsAny<int>())).ReturnsAsync("123456");

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(accountPlansServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IPaymentGateway>());
                    services.AddSingleton(cloverServiceMock.Object);
                    services.AddSingleton(Mock.Of<IPromotionRepository>());
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(Mock.Of<ISapService>());
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(GetCloverSettingsMock().Object);
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                    services.AddSingleton(paymentServiceMock.Object);
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518);

            // Act
            var response = await client.PostAsJsonAsync($"accounts/{accountName}/agreements", agreement);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static Mock<IOptions<CloverSettings>> GetCloverSettingsMock()
        {
            var cloverSettingsMock = new Mock<IOptions<CloverSettings>>();
            cloverSettingsMock.Setup(x => x.Value)
                .Returns(new CloverSettings
                {
                    UseCloverApi = true
                });

            return cloverSettingsMock;
        }

        private static Mock<IOptions<SlackSettings>> GetSlackSettingsMock()
        {
            var slackSettingsMock = new Mock<IOptions<SlackSettings>>();
            slackSettingsMock.Setup(x => x.Value)
                .Returns(new SlackSettings
                {
                    Url = "https://hooks.slack.com/services/test"
                });

            return slackSettingsMock;
        }

        private static Mock<IOptions<ZohoSettings>> GetZohoServiceSettingsMock()
        {
            var zohoSettingsMock = new Mock<IOptions<ZohoSettings>>();
            zohoSettingsMock.Setup(x => x.Value)
                .Returns(new ZohoSettings
                {
                    UseZoho = true
                });

            return zohoSettingsMock;
        }
    }
}
