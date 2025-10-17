using Dapper;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Test.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Doppler.BillingUser.Test
{
    public class BillingControllerTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> factory;
        const string TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518 = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOjEyMywidW5pcXVlX25hbWUiOiJ0ZXN0MUBleGFtcGxlLmNvbSIsInJvbGUiOiJVU0VSIiwiZXhwIjoyMDAwMDAwMDAwfQ.C4shc2SZqolHSpxSLU3GykR0A0Zyh0fofqNirS3CmeY4ZerofgRry7m9AMFyn1SG-rmLDpFJIObFA2dn7nN6uKf5gCTEIwGAB71LfAeVaEfOeF1SvLJh3-qGXknqinsrX8tuBhoaHmpWpvdp0PW-8PmLuBq-D4GWBGyrP73sx_qQi322E2_PJGfudygbahdQ9v4SnBh7AOlaLKSXhGRT-qsMCxZJXpHM7cZsaBkOlo8x_LEWbbkf7Ub6q3mWaQsR30NlJVTaRMY9xWrRMV_iZocREg2EI33mMBa5zhuyQ-hXENp5M9FgS_9B-j3LpFJoJyVFZG2beBRxU8tnqKan3A";

        public BillingControllerTest(WebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task Should_call_SendActivatedStandByEmail()
        {
            // Arrange
            var user = new User()
            {
                Language = "es",
                FirstName = "TestName",
                Email = "test1@example.com"
            };

            var currentPaymentMethod = new PaymentMethod
            {
                CCExpMonth = "1",
                CCExpYear = "2022",
                CCHolderFullName = "Test",
                CCNumber = "411111111111"
            };

            var templateId = "35ef4282-fd2b-45fa-8b9f-aec5082777d9";
            IEnumerable<string> to = new[] { user.Email };
            string replyTo = "replyTo@example.com";

            var userRepository = new Mock<IUserRepository>();
            userRepository
                .Setup(x => x.GetUserCurrentTypePlan(It.IsAny<int>()))
                .ReturnsAsync(null as UserTypePlanInformation);
            userRepository
                .Setup(x => x.GetUserBillingInformation("test1@example.com"))
                .ReturnsAsync(new UserBillingInformation() { Email = user.Email, PaymentMethod = PaymentMethodEnum.CC, IdBillingCountry = 1 });
            userRepository
                .Setup(x => x.GetUserNewTypePlan(1))
                .ReturnsAsync(new UserTypePlanInformation { IdUserType = UserTypeEnum.SUBSCRIBERS });
            userRepository
                .Setup(x => x.GetUserInformation("test1@example.com"))
                .ReturnsAsync(user);

            var accountPlanService = new Mock<IAccountPlansService>();
            accountPlanService
                .Setup(x => x.IsValidTotal("test1@example.com", It.IsAny<AgreementInformation>()))
                .ReturnsAsync(true);

            var billingCreditId = 1;

            var billingRepositoryMock = new Mock<IBillingRepository>();
            billingRepositoryMock.Setup(x => x.CreateBillingCreditAsync(It.IsAny<BillingCreditAgreement>())).ReturnsAsync(billingCreditId);
            billingRepositoryMock.Setup(x => x.GetPaymentMethodByUserName(It.IsAny<string>())).ReturnsAsync(currentPaymentMethod);
            billingRepositoryMock
                .Setup(x => x.ActivateStandBySubscribers(0))
                .ReturnsAsync(100);

            billingRepositoryMock
                .Setup(x => x.GetPlanDiscountInformation(3))
                .ReturnsAsync(new PlanDiscountInformation()
                {
                    DiscountPlanFee = 0,
                    MonthPlan = 1
                });

            var emailSenderMock = new Mock<IEmailSender>();
            emailSenderMock
                .Setup(x => x.SafeSendWithTemplateAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<Attachment>>(),
                    It.IsAny<CancellationToken>()));

            var encryptionServiceMock = new Mock<IEncryptionService>();
            encryptionServiceMock.Setup(x => x.DecryptAES256(It.IsAny<string>())).Returns("12345");

            var authorizationNumber = "LLLTD222";
            var paymentGatewayMock = new Mock<IPaymentGateway>();
            paymentGatewayMock
                .Setup(x => x.CreateCreditCardPayment(
                    It.IsAny<decimal>(),
                    It.IsAny<CreditCard>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(authorizationNumber);

            var sapServiceMock = new Mock<ISapService>();
            sapServiceMock
                .Setup(x => x.SendBillingToSap(It.IsAny<SapBillingDto>(), It.IsAny<string>()));

            var slackSettingsMock = new Mock<IOptions<SlackSettings>>();
            slackSettingsMock
                .Setup(x => x.Value)
                .Returns(new SlackSettings
                {
                    Url = "https://hooks.slack.com/services/test"
                });

            var relayEmailSenderSettingsMock = new Mock<IOptions<RelayEmailSenderSettings>>();
            relayEmailSenderSettingsMock
                .Setup(x => x.Value)
                .Returns(new RelayEmailSenderSettings
                {
                    ReplyToAddress = replyTo
                });

            var chatPlanUserRepositoryMock = new Mock<IChatPlanUserRepository>();
            chatPlanUserRepositoryMock.Setup(x => x.GetCurrentPlan(It.IsAny<string>())).Returns(Task.FromResult((CurrentPlan)null));

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(encryptionServiceMock.Object);
                    services.AddSingleton(userRepository.Object);
                    services.AddSingleton(accountPlanService.Object);
                    services.AddSingleton(billingRepositoryMock.Object);
                    services.AddSingleton(emailSenderMock.Object);
                    services.AddSingleton(paymentGatewayMock.Object);
                    services.AddSingleton(sapServiceMock.Object);
                    services.AddSingleton(slackSettingsMock.Object);
                    services.AddSingleton(relayEmailSenderSettingsMock.Object);
                    services.AddSingleton(Mock.Of<IUserPaymentHistoryRepository>());
                    services.AddSingleton(chatPlanUserRepositoryMock.Object);
                });

            }).CreateClient(new WebApplicationFactoryClientOptions());

            var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/test1@example.com/agreements")
            {
                Headers = { { "Authorization", $"Bearer {TOKEN_ACCOUNT_123_TEST1_AT_EXAMPLE_DOT_COM_EXPIRE_20330518}" } },
                Content = JsonContent.Create(new AgreementInformation()
                {
                    OriginInbound = "test",
                    Total = 0,
                    PlanId = 1,
                    DiscountId = 3
                })
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            emailSenderMock.Verify(x =>
                x.SafeSendWithTemplateAsync(
                    templateId,
                    It.IsAny<object>(),
                    to,
                    null,
                    null,
                    null,
                    null,
                    replyTo,
                    null,
                    default(CancellationToken)),
                Times.Once());
        }
    }
}
