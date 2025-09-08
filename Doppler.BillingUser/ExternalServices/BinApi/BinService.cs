using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.BinApi.Responses;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.BinApi
{
    public class BinService : IBinService
    {
        private readonly IOptions<BinSettings> _options;
        private readonly IFlurlClient _flurlClient;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        private readonly string[] validCreditCard = ["MASTERCARD", "VISA", "AMERICAN EXPRESS"];

        public BinService(
            IOptions<BinSettings> options,
            IFlurlClientFactory flurlClientFactory,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _options = options;
            _flurlClient = flurlClientFactory.Get(_options.Value.BaseUrl);
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<IsAllowedCreditCardResponse> IsAllowedCreditCard(string cardNumber)
        {
            try
            {
                var response = new IsAllowedCreditCardResponse { IsValid = true };

                var binApiResponse = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/bincheck")
                                .Resolve())
                                .AllowHttpStatus("404")
                                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                                .PostJsonAsync(new { bin = cardNumber });

                if (binApiResponse.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    throw new CardNotFoundException();
                }

                var binData = await binApiResponse.GetJsonAsync<BankIdentificationNumberResponse>();

                // TODO: Reinstate debit card check when business decision is made.
                //if (binData.Type == "DEBIT")
                //{
                //    response = new IsAllowedCreditCardResponse
                //    {
                //        IsValid = false,
                //        ErrorCode = "IsNotCreditCard"
                //    };
                //}
                //else
                //{
                if (!validCreditCard.Contains(binData.Scheme))
                {
                    response = new IsAllowedCreditCardResponse
                    {
                        IsValid = false,
                        ErrorCode = "IsNotAllowCreditCard"
                    };
                }
                //}

                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
