using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Dapper;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class GetCurrentPaymentMethodTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";
        private const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20010908 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoxMDAwMDAwMDAwfQ.Ite0xcvR2yLyFuVSBpoXeyJiwW44rYGJPGSX6VH_mCHImovvHMlcqJZkJLFy7A7jdUWJRZy23E_7tBR_rSEz9DBisiVksPeNqjuM3auUSZkPrRIYz16RZzLahhVNF-101j4Hg0Q7ZJB4zcT2a9qgB00CtSejUKrLoVljGj6mUz-ejVY7mNvUs0EE6e3pq4sylz9HHw0wZMBkv29xj_iE_3jBGwAwifh2UMQuBP_TAo6IiMaCMxmbPdITNEmQfXXIG3yPw6KwRjDw_EWR_R6yWFhbXuLONsZQF6b9mfokW9PxQ5MNCgvXihWCYaAibJ62R3N0pyUuvpjOJfifwFFaRA";
        private const string TOKEN_PROVISORY_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_1983727216 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJwcm92aXNvcnlfdW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsImlhdCI6MTY2ODEwODAxNiwiZXhwIjoxOTgzNzI3MjE2fQ.ej2ZvwjVks1B2CEwPGalEWBxIj995-W4CXNYpAuBS4_USJ2WKJX2tFG6BS7CkAznMYvIom1Unym6vFSaKdQTlP38VwvEohr9FZA5MiLolB1Owddl5Qbu7VXOAChyO14055LvMeJD3aj1HturtndhU_Qv6z9Q29L7Qk2cUN5SjeQgnn9lAxf0nHVG4y-3Q-9tX75EBT4Y5HVHfncvGat4b1K-K8bOC9eXFcdAALIHHsDwWxhOsx_0HMlfBJLc0ThaIiJ0zr2Z-h6jbbfsE6eUHloIZ85ptHl7inLW4j0Wi9jDsYHVjOdgEDwzKhf-U-Uc-3G2rvp9tvhSS0vbDdt0xw";
        private readonly WebApplicationFactory<Startup> _factory;

        public GetCurrentPaymentMethodTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GET_current_payment_method_should_authorize_provisory_user()
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
        public async Task GET_current_payment_method_should_return_forbidden_if_provisory_user_tries_to_access_resources_not_owned()
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
        public async Task GET_current_payment_method_should_be_return_unauthorized_When_token_is_invalid(string token)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/payment-methods/current")
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GET_current_payment_method_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/payment-methods/current");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        [Fact]
        public async Task GET_current_payment_method_should_get_right_values_when_username_is_valid()
        {
            // Arrange
            const string expectedContent = "{\"ccHolderFullName\":\"TEST\",\"ccNumber\":\"TEST\",\"ccVerification\":\"****\",\"ccExpMonth\":\"2\",\"ccExpYear\":\"2130\",\"ccType\":\"Visa\",\"paymentMethodName\":\"CC\",\"renewalMonth\":\"6\",\"razonSocial\":\"Company\",\"idConsumerType\":\"NC\",\"identificationType\":\"DNI\",\"identificationNumber\":\"344444\",\"idSelectedPlan\":1,\"responsableIVA\":true,\"idCCType\":0,\"useCFDI\":null,\"paymentType\":null,\"paymentWay\":null,\"bankName\":null,\"bankAccount\":null}";
            var paymentMethod = new PaymentMethod
            {
                PaymentMethodName = "CC",
                CCNumber = "41111111",
                CCExpMonth = "2",
                CCExpYear = "2130",
                CCHolderFullName = "Test holder Name",
                IdentificationNumber = "344444",
                IdentificationType = "DNI",
                CCType = "Visa",
                CCVerification = "213",
                IdConsumerType = "9",
                RazonSocial = "Company",
                RenewalMonth = "6",
                IdSelectedPlan = 1,
                ResponsableIVA = true
            };

            var mockConnection = new Mock<DbConnection>();
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<PaymentMethod>(null, null, null, null, null))
                .ReturnsAsync(paymentMethod);

            var encryptedMock = new Mock<IEncryptionService>();
            encryptedMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("TEST");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.SetupConnectionFactory(mockConnection.Object);
                    services.AddSingleton(encryptedMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Get, "accounts/test1@example.com/payment-methods/current")
            {
                Headers =
                {
                    {
                        "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}"
                    }
                }
            };

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expectedContent, responseContent);
        }

    }
}
