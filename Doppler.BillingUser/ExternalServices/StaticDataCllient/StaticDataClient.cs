using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Model;
using Flurl.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.StaticDataCllient
{
    public class StaticDataClient : IStaticDataClient
    {
        private readonly ILogger<StaticDataClient> _logger;
        private readonly IOptions<StaticDataClientSettings> _options;

        public StaticDataClient(ILogger<StaticDataClient> logger, IOptions<StaticDataClientSettings> options)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<GetTaxRegimesResult> GetAllTaxRegimesAsync()
        {
            HttpClient staticDataHttpClient = new HttpClient()
            {
                BaseAddress = new Uri(_options.Value.BaseStaticDataClientUrl)
            };

            try
            {
                var result = new List<TaxRegime>();

                var response = await staticDataHttpClient.GetAsync("/static-data/tax-regimes-es.json");

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;

                    JToken contentParsed = JToken.Parse(content);

                    foreach (var item in contentParsed.ToKeyValuePairs())
                    {
                        result.Add(new TaxRegime() { Id = Int32.Parse(item.Key), Description = (string)item.Value });
                    }

                    return new GetTaxRegimesResult() { IsSuccessful = true, TaxRegimes = result };
                }
                else
                {
                    _logger.LogError(string.Format("Status code should be 200, so something went wrong consuming the StaticData Api, status code returned: {0}", response.StatusCode));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(string.Format("Unexpected error getting data from SitesApi, Exception:{0}", e));
            }

            return new GetTaxRegimesResult() { IsSuccessful = false };
        }
    }
}
