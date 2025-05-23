using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using Newtonsoft.Json;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http.Testing;
using Microsoft.Extensions.Options;
using Xunit;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.ExternalServices.BinApi;
using Doppler.BillingUser.ExternalServices.BinApi.Responses;

namespace Doppler.BillingUser.Test
{
    public class PutCurrentPaymentMethodTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private const string TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJwcm92aXNvcnlfdW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsImlhdCI6MTY2ODEwODAxNiwiZXhwIjoxOTgzNzI3MjE2fQ.ej2ZvwjVks1B2CEwPGalEWBxIj995-W4CXNYpAuBS4_USJ2WKJX2tFG6BS7CkAznMYvIom1Unym6vFSaKdQTlP38VwvEohr9FZA5MiLolB1Owddl5Qbu7VXOAChyO14055LvMeJD3aj1HturtndhU_Qv6z9Q29L7Qk2cUN5SjeQgnn9lAxf0nHVG4y-3Q-9tX75EBT4Y5HVHfncvGat4b1K-K8bOC9eXFcdAALIHHsDwWxhOsx_0HMlfBJLc0ThaIiJ0zr2Z-h6jbbfsE6eUHloIZ85ptHl7inLW4j0Wi9jDsYHVjOdgEDwzKhf-U-Uc-3G2rvp9tvhSS0vbDdt0xw";
        private readonly WebApplicationFactory<Startup> _factory;
        public PutCurrentPaymentMethodTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PUT_current_payment_method_should_authorize_provisory_user()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/payment-methods/current")
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
        public async Task PUT_current_payment_method_should_return_forbidden_if_provisory_user_tries_to_access_resources_not_owned()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test2@example.com/payment-methods/current")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData(TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908)]
        [InlineData("")]
        [InlineData("invalid")]
        public async Task PUT_Current_payment_method_should_be_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payment-methods/current")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_method_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payment-methods/current");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_CC_method_should_return_bad_request_when_use_cc_method_and_the_card_is_not_allowed()
        {
            // Arrange
            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("Test Holder Name"), "CCHolderFullName" },
                { new StringContent("4444 4444 4444 4444"), "CCNumber" },
                { new StringContent("222"), "CCVerification" },
                { new StringContent("12"), "CCExpMonth" },
                { new StringContent("25"), "CCExpYear" },
                { new StringContent("Mastercard"), "CCType" },
                { new StringContent(PaymentMethodEnum.CC.ToString()), "PaymentMethodName" },
                { new StringContent("13"), "IdSelectedPlan" }
            };

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    PaymentMethod = 1,
                    IsCancelated = false
                });

            var binServiceMock = new Mock<IBinService>();
            binServiceMock.Setup(x => x.IsAllowedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(new IsAllowedCreditCardResponse { IsValid = false });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(binServiceMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_CC_method_should_update_right_value_based_on_body_information()
        {
            // Arrange

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("Test Holder Name"), "CCHolderFullName" },
                { new StringContent("5555 5555 5555 5555"), "CCNumber" },
                { new StringContent("222"), "CCVerification" },
                { new StringContent("12"), "CCExpMonth" },
                { new StringContent("25"), "CCExpYear" },
                { new StringContent("Mastercard"), "CCType" },
                { new StringContent(PaymentMethodEnum.CC.ToString()), "PaymentMethodName" },
                { new StringContent("13"), "IdSelectedPlan" }
            };

            var user = new User
            {
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(currentPaymentMethod), Encoding.UTF8, "application/json");

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.IsValidCreditCard(It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendUserDataToSap(It.IsAny<SapBusinessPartner>(), null));

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    PaymentMethod = 1,
                    IsCancelated = false
                });
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(new UserBillingInformation()
            {
                IdCurrentBillingCredit = 1
            });

            var binServiceMock = new Mock<IBinService>();
            binServiceMock.Setup(x => x.IsAllowedCreditCard(It.IsAny<string>()))
                .ReturnsAsync(new IsAllowedCreditCardResponse { IsValid = true });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(binServiceMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_CC_method_should_update_right_value_based_on_body_information_when_CardNotFoundException_is_thrown()
        {
            // Arrange

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("Test Holder Name"), "CCHolderFullName" },
                { new StringContent("5555 5555 5555 5555"), "CCNumber" },
                { new StringContent("222"), "CCVerification" },
                { new StringContent("12"), "CCExpMonth" },
                { new StringContent("25"), "CCExpYear" },
                { new StringContent("Mastercard"), "CCType" },
                { new StringContent(PaymentMethodEnum.CC.ToString()), "PaymentMethodName" },
                { new StringContent("13"), "IdSelectedPlan" }
            };

            var user = new User
            {
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(currentPaymentMethod), Encoding.UTF8, "application/json");

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.IsValidCreditCard(It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(true);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendUserDataToSap(It.IsAny<SapBusinessPartner>(), null));

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    IdUser = 1,
                    PaymentMethod = 1,
                    IsCancelated = false
                });
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(new UserBillingInformation()
            {
                IdCurrentBillingCredit = 1
            });

            var binServiceMock = new Mock<IBinService>();
            binServiceMock.Setup(x => x.IsAllowedCreditCard(It.IsAny<string>()))
                .ThrowsAsync(new CardNotFoundException());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(binServiceMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_Transfer_method_should_update_right_value_based_on_body_information()
        {
            // Arrange
            const int userId = 1;
            const int expectedRows = 1;

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("TRANSF"), "PaymentMethodName" },
                { new StringContent("13"), "IdSelectedPlan" },
                { new StringContent("test"), "RazonSocial" },
                { new StringContent("RI"), "IdConsumerType" },
                { new StringContent("2334345566"), "IdentificationNumber" },

            };

            var user = new User
            {
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}"
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(currentPaymentMethod), Encoding.UTF8, "application/json");

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null)).ReturnsAsync(userId);
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendUserDataToSap(It.IsAny<SapBusinessPartner>(), null));

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                            .ReturnsAsync(new User()
                            {
                                IdUser = 1,
                                PaymentMethod = 1,
                                IsCancelated = false
                            });
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(new UserBillingInformation() { IdCurrentBillingCredit = 0 });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_Transfer_method_should_not_sent_to_sap_with_right_value_based_on_body_information()
        {
            // Arrange
            const int userId = 1;
            const int expectedRows = 1;

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("TRANSF"), "PaymentMethodName"},
                { new StringContent("13"), "IdSelectedPlan"},
                { new StringContent("test"), "RazonSocial"},
                { new StringContent("RI"), "IdConsumerType"},
                { new StringContent("2334345566"), "IdentificationNumber"}
            };

            var user = new User
            {
                Email = "test1@example.com",
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}",
                CUIT = "2334345566",
                IdConsumerType = 2,
                IdResponsabileBilling = 9,
                FirstName = "firstName"
            };

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null)).ReturnsAsync(userId);
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                            .ReturnsAsync(new User()
                            {
                                IdUser = 1,
                                PaymentMethod = 1,
                                IsCancelated = false
                            });
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(new UserBillingInformation() { IdCurrentBillingCredit = 0 });

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(GetSapSettingsMock().Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");
            var httpTest = new HttpTest();
            const string url = "https://localhost:5000/businesspartner/createorupdatebusinesspartner";

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            httpTest.ShouldNotHaveCalled(url);
        }

        [Fact]
        public async Task PUT_Current_payment_CC_method_should_return_bad_request_when_CC_is_invalid()
        {
            // Arrange
            const int userId = 1;

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("Test Holder Name"), "CCHolderFullName" },
                { new StringContent("5555 5555 5555 5555"), "CCNumber" },
                { new StringContent("222"), "CCVerification" },
                { new StringContent("12"), "CCExpMonth" },
                { new StringContent("25"), "CCExpYear" },
                { new StringContent("Mastercard"), "CCType" },
                { new StringContent(PaymentMethodEnum.CC.ToString()), "PaymentMethodName" },
                { new StringContent("13"), "IdSelectedPlan" }
            };

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null))
                .ReturnsAsync(userId);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock.Setup(x => x.IsValidCreditCard(It.IsAny<CreditCard>(), It.IsAny<int>(), It.IsAny<bool>())).ReturnsAsync(false);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(Mock.Of<IUserRepository>());
                    services.AddSingleton(GetSapSettingsMock().Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_Mercadopago_method_should_update_right_value_based_on_body_information()
        {
            // Arrange
            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("Test Holder Name"), "CCHolderFullName" },
                { new StringContent("5555 5555 5555 5555"), "CCNumber" },
                { new StringContent("222"), "CCVerification" },
                { new StringContent("12"), "CCExpMonth" },
                { new StringContent("25"), "CCExpYear" },
                { new StringContent("Mastercard"), "CCType" },
                { new StringContent(PaymentMethodEnum.MP.ToString()), "PaymentMethodName" },
                { new StringContent("13"), "IdSelectedPlan" },
                { new StringContent("2334345566"), "IdentificationNumber" }
            };

            var user = new User
            {
                Email = "test1@example.com",
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}",
                CUIT = "2334345566",
                IdConsumerType = 2,
                IdResponsabileBilling = (int)ResponsabileBillingEnum.Mercadopago,
                FirstName = "firstName"
            };

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                            .ReturnsAsync(new User()
                            {
                                IdUser = 1,
                                PaymentMethod = 1,
                                IsCancelated = false
                            });
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(new UserBillingInformation() { IdCurrentBillingCredit = 0 });

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(GetSapSettingsMock().Object);
                    services.AddSingleton(userRepositoryMock.Object);
                });

            });
            factory.Server.PreserveExecutionContext = true;
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");
            var httpTest = new HttpTest();
            const string url = "https://localhost:5000/businesspartner/createorupdatebusinesspartner";

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            httpTest.ShouldHaveCalled(url);
        }

        [Fact]
        public async Task PUT_Current_payment_CC_method_should_return_bad_request_when_User_is_cancelated()
        {
            // Arrange
            const int userId = 1;

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("Test Holder Name"), "CCHolderFullName" },
                { new StringContent("5555 5555 5555 5555"), "CCNumber" },
                { new StringContent("222"), "CCVerification" },
                { new StringContent("12"), "CCExpMonth" },
                { new StringContent("25"), "CCExpYear" },
                { new StringContent("Mastercard"), "CCType" },
                { new StringContent(PaymentMethodEnum.CC.ToString()), "PaymentMethodName" },
                { new StringContent("13"), "IdSelectedPlan" },
                { new StringContent("2334345566"), "IdentificationNumber" }
            };

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null))
                .ReturnsAsync(userId);

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                            .ReturnsAsync(new User()
                            {
                                IdUser = 1,
                                PaymentMethod = 1,
                                IsCancelated = true
                            });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(Mock.Of<IEncryptionService>());
                    services.AddSingleton(Mock.Of<IPaymentGateway>());
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(GetSapSettingsMock().Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Current_payment_AutomaticDebit_method_should_return_bad_request_when_cbu_isinvalid()
        {
            // Arrange
            const int userId = 1;
            const int expectedRows = 1;

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("DA"), "PaymentMethodName" },
                { new StringContent("1194"), "IdSelectedPlan" },
                { new StringContent("test"), "RazonSocial" },
                { new StringContent("CF"), "IdConsumerType" },
                { new StringContent("20123456"), "IdentificationNumber" },
                { new StringContent("1234567890123456789012"), "Cbu" },
            };

            var user = new User
            {
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}"
            };

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null)).ReturnsAsync(userId);
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendUserDataToSap(It.IsAny<SapBusinessPartner>(), null));

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                            .ReturnsAsync(new User()
                            {
                                IdUser = 1,
                                PaymentMethod = 1,
                                IsCancelated = false
                            });
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(new UserBillingInformation() { IdCurrentBillingCredit = 0 });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(Mock.Of<IPayrollOfBCRAEntityRepository>());
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            userRepositoryMock.Verify(x => x.GetEncryptedCreditCard(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task PUT_Current_payment_AutomaticDebit_method_should_update_right_value_based_on_body_information()
        {
            // Arrange
            const int userId = 1;
            const int expectedRows = 1;

            var currentPaymentMethod = new MultipartFormDataContent()
            {
                { new StringContent("DA"), "PaymentMethodName" },
                { new StringContent("1194"), "IdSelectedPlan" },
                { new StringContent("test"), "RazonSocial" },
                { new StringContent("CF"), "IdConsumerType" },
                { new StringContent("20123456"), "IdentificationNumber" },
                { new StringContent("4292116911100027125776"), "Cbu" },
            };

            var user = new User
            {
                SapProperties = "{\"ContractCurrency\" : false,\"GovernmentAccount\" : false,\"Premium\" : false,\"Plus\" : false,\"ComercialPartner\" : false,\"MarketingPartner\" : false,\"OnBoarding\" : false,\"Layout\" : false,\"Datahub\" : false,\"PushNotification\" : false,\"ExclusiveIp\" : false,\"Advisory\" : false,\"Reports\" : false,\"SMS\" : false}"
            };

            var mockConnection = new Mock<DbConnection>();

            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<int>(null, null, null, null, null)).ReturnsAsync(userId);
            mockConnection.SetupDapperAsync(c => c.ExecuteAsync(null, null, null, null, null)).ReturnsAsync(expectedRows);
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(null, null, null, null, null)).ReturnsAsync(user);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock.Setup(x => x.SendUserDataToSap(It.IsAny<SapBusinessPartner>(), null));

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var userRepositoryMock = new Mock<IUserRepository>();
            userRepositoryMock.Setup(x => x.GetUserInformation(It.IsAny<string>()))
                            .ReturnsAsync(new User()
                            {
                                IdUser = 1,
                                PaymentMethod = 1,
                                IsCancelated = false
                            });
            userRepositoryMock.Setup(x => x.GetUserBillingInformation(It.IsAny<string>())).ReturnsAsync(new UserBillingInformation() { IdCurrentBillingCredit = 0 });

            var payrollOfBCRAEntityRepositoryMock = new Mock<IPayrollOfBCRAEntityRepository>();
            payrollOfBCRAEntityRepositoryMock.Setup(x => x.GetByBankCode(It.IsAny<string>())).ReturnsAsync(new PayrollOfBCRAEntity());

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(userRepositoryMock.Object);
                    services.AddSingleton(payrollOfBCRAEntityRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}");

            // Act
            var response = await client.PutAsync("accounts/test1@example.com/payment-methods/current", currentPaymentMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            userRepositoryMock.Verify(x => x.GetEncryptedCreditCard(It.IsAny<string>()), Times.Never());
        }

        private static Mock<IOptions<SapSettings>> GetSapSettingsMock()
        {
            var accountPlansSettingsMock = new Mock<IOptions<SapSettings>>();
            accountPlansSettingsMock.Setup(x => x.Value)
                .Returns(new SapSettings
                {
                    SapBaseUrl = "https://localhost:5000/",
                    SapCreateBusinessPartnerEndpoint = "businesspartner/createorupdatebusinesspartner"
                });

            return accountPlansSettingsMock;
        }
    }
}
