using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.PaymentsApi.Responses;
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

        public PaymentsService(
            ILogger<PaymentsService> logger,
            IOptions<PaymentsSettings> options,
            IFlurlClientFactory flurlClientFactory,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _options = options;
            _flurlClient = flurlClientFactory.Get(_options.Value.BaseUrl);
            _logger = logger;
            _jwtTokenGenerator = jwtTokenGenerator;
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

        public async Task<string> Purchase(string paymentToken, decimal amount)
        {
            try
            {
                var body = new { token = paymentToken, amount };

                _logger.LogInformation($"Purchase - Json request: {JsonConvert.SerializeObject(body)}");

                var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/purchase")
                                .Resolve())
                                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                                .PostJsonAsync(body)
                                .ReceiveJson<PurchaseResponse>();

                _logger.LogInformation($"Purchase - Json response: {JsonConvert.SerializeObject(response)}");

                if (response?.CreditPurchaseResponse == null)
                {
                    throw new Exception("Payment API returned null or invalid response");
                }

                if (!response.CreditPurchaseResponse.IsSuccessful)
                {
                    var errorMessage = $"purchase failed. ReturnCode: {response.CreditPurchaseResponse.ReturnCode}, " +
                                        $"ReasonCode: {response.CreditPurchaseResponse.ReasonCode}, " +
                                        $"ResponseCode: {response.CreditPurchaseResponse.ResponseCode}, ";
                    throw new Exception(errorMessage);
                }

                return response?.CreditPurchaseResponse.ReferenceTraceNumbers.AuthorizationNumber;
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
    }
}
