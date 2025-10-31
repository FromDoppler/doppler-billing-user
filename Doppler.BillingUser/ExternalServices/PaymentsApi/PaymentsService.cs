using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.Clover.Requests;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.PaymentsApi.Exceptions;
using Doppler.BillingUser.ExternalServices.PaymentsApi.Requests;
using Doppler.BillingUser.ExternalServices.PaymentsApi.Responses;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Services;
using Flurl.Http;
using Flurl.Http.Configuration;
using Loggly.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi
{
    public class PaymentsService : IPaymentsService
    {
        private readonly IOptions<PaymentsSettings> _options;
        private readonly IFlurlClient _flurlClient;
        private readonly ILogger _logger;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly IEncryptionService _encryptionService;
        private readonly ISlackService _slackService;

        public PaymentsService(
            ILogger<PaymentsService> logger,
            IOptions<PaymentsSettings> options,
            IFlurlClientFactory flurlClientFactory,
            IJwtTokenGenerator jwtTokenGenerator,
            IEmailTemplatesService emailTemplatesService,
            IEncryptionService encryptionService,
            ISlackService slackService)
        {
            _options = options;
            _flurlClient = flurlClientFactory.Get(_options.Value.BaseUrl);
            _logger = logger;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailTemplatesService = emailTemplatesService;
            _encryptionService = encryptionService;
            _slackService = slackService;
        }

        public async Task<string> GeneratePaymentToken(string WorldPayLowValueToken)
        {
            try
            {
                var body = new { worldPayLowValueToken = WorldPayLowValueToken };

                _logger.LogInformation($"Authorization - Json request: {JsonConvert.SerializeObject(body)}");

                var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/authorization")
                                .Resolve())
                                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                                .PostJsonAsync(body)
                                .ReceiveJson<AuthorizationResponse>();

                _logger.LogInformation($"Authorization - Json response: {JsonConvert.SerializeObject(response)}");

                if (response?.CreditAuthResponse == null)
                {
                    throw new Exception("Payment API returned null or invalid response");
                }

                var authResponse = response.CreditAuthResponse;

                if (!authResponse.IsSuccessful)
                {
                    var errorMessage = $"Payment token generation failed. ReturnCode: {authResponse.ReturnCode}, " +
                                        $"ReasonCode: {authResponse.ReasonCode}, " +
                                        $"ResponseCode: {authResponse.ResponseCode}, " +
                                        $"ReturnText: {authResponse.ReturnText ?? "N/A"}";
                    throw new Exception(errorMessage);
                }

                if (authResponse.EncryptionTokenData == null || string.IsNullOrEmpty(authResponse.EncryptionTokenData.TokenizedPan))
                {
                    throw new Exception("Payment API did not return a tokenized PAN");
                }

                return authResponse.EncryptionTokenData.TokenizedPan;
            }
            catch (FlurlHttpException ex)
            {
                var errorMessage = $"HTTP error calling Payment API: {ex.StatusCode}";
                var responseBody = await ex.GetResponseStringAsync();
                errorMessage += $", Response: {responseBody}";

                throw new Exception(errorMessage, ex);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> Purchase(string paymentToken, decimal amount, string accountname, CreditCard creditCard, int clientId, bool isFreeUser, string lastFourDigitsCCNumber)
        {
            try
            {
                var request = new PurchaseRequest
                {
                    PaymentToken = paymentToken,
                    Amount = amount,
                    CustomerEmail = accountname,
                    CustomerId = clientId,
                    EncryptedCreditCard = creditCard
                };

                _logger.LogInformation($"Purchase - Json request: {JsonConvert.SerializeObject(request)}");

                var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/purchase")
                                .Resolve())
                                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                                .PostJsonAsync(request)
                                .ReceiveJson<PurchaseResponse>();

                _logger.LogInformation($"Purchase - Json response: {JsonConvert.SerializeObject(response)}");

                if (response == null)
                {
                    throw new Exception("Payment API returned null or invalid response");
                }

                if (!response.IsSuccessful)
                {
                    var errorMessage = $"purchase failed. ResponseCode: {response.ResponseCode}, ";
                    throw new Exception(errorMessage);
                }

                return response?.AuthorizationNumber;
            }
            catch (FlurlHttpException ex)
            {
                var errorReponseBody = await ex.GetResponseJsonAsync<PaymentError>();

                await _emailTemplatesService.SendNotificationForPaymentFailedTransaction(clientId, errorReponseBody.ErrorCode, errorReponseBody.ErrorMessage, errorReponseBody.TransactionCTR, errorReponseBody.BankMessage, PaymentMethodEnum.CC, isFreeUser, string.Empty, lastFourDigitsCCNumber);

                var messageError = $"Failed to create the payment for user {accountname} {errorReponseBody.ErrorCode}: {errorReponseBody.ErrorMessage}.";
                await _slackService.SendNotification(messageError);

                throw new Exception(errorReponseBody.ErrorMessage);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
