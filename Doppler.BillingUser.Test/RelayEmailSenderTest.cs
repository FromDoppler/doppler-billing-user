using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.TimeCollector;
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
    public class RelayEmailSenderTest
    {
        private const string SendTemplateUrl = "https://api.dopplerrelay.com/accounts/{accountId}/templates/{templateId}/message";

        [Fact]
        public async Task SendWithTemplateAsync()
        {
            // Arrange
            var apiKey = "apiKey";
            var relayAccountId = It.IsAny<int>();
            var relayAccountName = It.IsAny<string>();
            var relayUserEmail = "salesrelay@dopplerrelay.com";
            var templateId = It.IsAny<string>();
            var demoData = It.IsAny<string>();
            var toEmail = "email@example.com";
            var replyToAddress = "email@example.com";
            var bccEmail = "copy@example.com";
            var expectedUrl = $"https://api.dopplerrelay.com/accounts/{relayAccountId}/templates/{templateId}/message";

            var configuration = new RelayEmailSenderSettings()
            {
                SendTemplateUrlTemplate = SendTemplateUrl,
                ApiKey = apiKey,
                AccountId = relayAccountId,
                AccountName = relayAccountName,
                Username = relayUserEmail,
                ReplyToAddress = replyToAddress
            };

            IFlurlClientFactory factory = new PerBaseUrlFlurlClientFactory();
            var sut = new RelayEmailSender(Options.Create(configuration), Mock.Of<ILogger<RelayEmailSender>>(), factory, Mock.Of<ITimeCollector>());

            using (var httpTest = new HttpTest())
            {
                // Act
                await sut.SendWithTemplateAsync(
                    templateId,
                    new { demoData = demoData },
                    new[] { toEmail },
                    bcc: new[] { bccEmail },
                    replyTo: configuration.ReplyToAddress);

                // Assert
                httpTest
                    .ShouldHaveCalled(expectedUrl)
                    .WithVerb(HttpMethod.Post)
                    .WithOAuthBearerToken(apiKey)
                    .WithRequestJson(new
                    {
                        from_name = (string)null,
                        from_email = (string)null,
                        recipients = new[]
                        {
                            new { email = toEmail, type = "to" },
                            new { email = bccEmail, type = "bcc" },
                        },
                        attachments = (object)null,
                        model = new { demoData = demoData },
                        reply_to = new { email = configuration.ReplyToAddress, name = configuration.FromName }
                    });
            }
        }
    }
}
