using Doppler.BillingUser.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

                    var contentParsed = JObject.Parse(content);

                    foreach (var item in contentParsed.Properties())
                    {
                        result.Add(new TaxRegime() { Id = Int32.Parse(item.Name), Description = item.Value.ToString() });
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
