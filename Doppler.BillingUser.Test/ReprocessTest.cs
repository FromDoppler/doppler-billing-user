using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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
        public async Task PUT_Current_payment_method_should_return_unauthorized_when_authorization_is_empty()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Put, "accounts/test1@example.com/payments/reprocess");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
