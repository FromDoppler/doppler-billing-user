using Doppler.BillingUser.ExternalServices.BinApi;
using Doppler.BillingUser.ExternalServices.BinApi.Responses;
using Doppler.BillingUser.ExternalServices.PaymentsApi.Responses;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System;
using System.Threading.Tasks;
using Tavis.UriTemplates;
using Microsoft.AspNetCore.Http;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi
{
    public class PaymentsService : IPaymentsService
    {
        private readonly IOptions<PaymentsSettings> _options;
        private readonly IFlurlClient _flurlClient;

        public PaymentsService(IOptions<PaymentsSettings> options, IFlurlClientFactory flurlClientFactory)
        {
            _options = options;
            _flurlClient = flurlClientFactory.Get(_options.Value.BaseUrl);
        }

        public async Task<string> GeneratePaymentToken(string WorldPayLowValueToken, string CardNumber)
        {
            try
            {
                var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/token")
                                .Resolve())
                                .PostJsonAsync(new { cardNumber = CardNumber, worldPayLowValueToken = WorldPayLowValueToken })
                                .ReceiveJson<PaymentTokenResponse>();

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
    }
}
