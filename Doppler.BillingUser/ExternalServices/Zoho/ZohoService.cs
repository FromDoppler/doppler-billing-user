using Doppler.BillingUser.ExternalServices.Zoho.API;
using Doppler.BillingUser.TimeCollector;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.Zoho
{
    public class ZohoService : IZohoService
    {
        private readonly IOptions<ZohoSettings> _options;
        private readonly IFlurlClient _flurlZohoClient;
        private readonly IFlurlClient _flurlZohoAuthenticationClient;
        private readonly ITimeCollector _timeCollector;
        private string accessToken;

        public ZohoService(
            IOptions<ZohoSettings> options,
            IFlurlClientFactory flurlClientFac,
            ITimeCollector timeCollector)
        {
            _options = options;
            _flurlZohoClient = flurlClientFac.Get(_options.Value.BaseUrl);
            _flurlZohoAuthenticationClient = flurlClientFac.Get(_options.Value.AuthenticationUrl);
            _timeCollector = timeCollector;
        }

        public async Task RefreshTokenAsync()
        {
            using var _ = _timeCollector.StartScope();
            var response = await _flurlZohoAuthenticationClient.Request(new UriTemplate($"{_options.Value.AuthenticationUrl}").Resolve())
                .SetQueryParam("refresh_token", _options.Value.ZohoRefreshToken)
                .SetQueryParam("grant_type", "refresh_token")
                .SetQueryParam("scope", "ZohoCRM.modules.ALL,ZohoCRM.users.ALL")
                .SetQueryParam("client_id", _options.Value.ZohoClientId)
                .SetQueryParam("client_secret", _options.Value.ZohoClientSecret)
                .WithHeader("Authorization", $"Zoho-oauthtoken {_options.Value.ZohoRefreshToken}")
                .PostAsync().ReceiveJson<ZohoRefreshTokenResponse>();

            if (response != null)
            {
                accessToken = response.AccessToken;
            }
        }

        public async Task<T> SearchZohoEntityAsync<T>(string moduleName, string criteria)
        {
            using var _ = _timeCollector.StartScope();
            var entity = await _flurlZohoClient.Request(new UriTemplate($"{_options.Value.BaseUrl}{moduleName}/search").Resolve())
                .SetQueryParam("criteria", criteria)
                .WithHeader("Authorization", $"Zoho-oauthtoken {accessToken}")
                .GetJsonAsync<T>();

            return entity;
        }

        public async Task<ZohoUpdateResponse> UpdateZohoEntityAsync(string body, string entityId, string moduleName)
        {
            using var _ = _timeCollector.StartScope();
            var entity = await _flurlZohoClient.Request(new UriTemplate($"{_options.Value.BaseUrl}{moduleName}/{entityId}").Resolve())
                .WithHeader("Authorization", $"Zoho-oauthtoken {accessToken}")
                .PutStringAsync(body).ReceiveJson<ZohoUpdateResponse>();
            return entity;
        }
    }
}
