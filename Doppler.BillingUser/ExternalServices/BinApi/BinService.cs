using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.ExternalServices.BeplicApi;
using Doppler.BillingUser.ExternalServices.BeplicApi.Responses;
using Doppler.BillingUser.ExternalServices.BinApi.Responses;
using Doppler.BillingUser.Model;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.BinApi
{
    public class BinService : IBinService
    {
        private readonly IOptions<BinSettings> _options;
        //private readonly ILogger<BinService> _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public BinService(
            IOptions<BinSettings> options,
           /* ILogger<BinService> logger,*/
            IFlurlClientFactory flurlClientFactory,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _options = options;
            _flurlClient = flurlClientFactory.Get(_options.Value.BaseUrl);
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<bool> IsCreditCard(string cardNumber)
        {
            try
            {
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

                return binData.Type == "CREDIT";

            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
