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

        public async Task<string> GeneratePaymentToken(string WorldPayLowValueToken, string CardNumber)
        {
            try
            {
                var body = new { cardNumber = CardNumber, worldPayLowValueToken = WorldPayLowValueToken };

                _logger.LogInformation($"Token - Json request: {JsonConvert.SerializeObject(body)}");

                var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/token")
                                .Resolve())
                                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                                .PostJsonAsync(body)
                                .ReceiveJson<PaymentTokenResponse>();

                _logger.LogInformation($"Token - Json response: {JsonConvert.SerializeObject(response)}");

                if (response?.DeregistrationLohiResponse == null)
                {
                    throw new Exception("Payment API returned null or invalid response");
                }

                var lohiResponse = response.DeregistrationLohiResponse;

                if (!lohiResponse.IsSuccessful)
                {
                    var errorMessage = $"Payment token generation failed. ReturnCode: {lohiResponse.ReturnCode}, " +
                                        $"ReasonCode: {lohiResponse.ReasonCode}, " +
                                        $"ResponseCode: {lohiResponse.ResponseCode}, " +
                                        $"ReturnText: {lohiResponse.ReturnText ?? "N/A"}, " +
                                        $"ErrorInformation: {lohiResponse.ErrorInformation ?? "N/A"}";
                    throw new Exception(errorMessage);
                }

                if (lohiResponse.EncryptionTokenData == null || string.IsNullOrEmpty(lohiResponse.EncryptionTokenData.TokenizedPan))
                {
                    throw new Exception("Payment API did not return a tokenized PAN");
                }

                return lohiResponse.EncryptionTokenData.TokenizedPan;
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

        public async Task<string> Purchase(string paymentToken, decimal amount, string accountname, CreditCard creditCard, int clientId, bool isFreeUser)
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
                var errorReponseBody = await ex.GetResponseJsonAsync<PaymentException>();
                var cardHolderName = string.IsNullOrEmpty(paymentToken) ? _encryptionService.DecryptAES256(creditCard.HolderName) : string.Empty;
                var lastFourDigits = string.IsNullOrEmpty(paymentToken) ? _encryptionService.DecryptAES256(creditCard.Number)[^4..] : string.Empty;

                await _emailTemplatesService.SendNotificationForPaymentFailedTransaction(clientId, errorReponseBody.ErrorPayment.ErrorCode, errorReponseBody.ErrorPayment.ErrorMessage, errorReponseBody.ErrorPayment.TransactionCTR, errorReponseBody.ErrorPayment.BankMessage, PaymentMethodEnum.CC, isFreeUser, cardHolderName, lastFourDigits);

                var messageError = $"Failed to create the payment for user {accountname} {errorReponseBody.ErrorPayment.ErrorCode}: {errorReponseBody.ErrorPayment.ErrorMessage}.";
                await _slackService.SendNotification(messageError);

                return string.Empty;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
