using Doppler.BillingUser.Exceptions;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.DopplerApi
{
    public class DopplerMvcService : IDopplerMvcService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<DopplerSettings> _options;
        private readonly IFlurlClient _flurlClient;

        public const string MVC_SESSION_COOKIE_NAME = "ASP.NET_SessionId";

        public DopplerMvcService(
            IHttpContextAccessor httpContextAccessor,
            IOptions<DopplerSettings> options,
            IFlurlClientFactory flurlClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options;
            _flurlClient = flurlClientFactory.Get(_options.Value.MvcUrl);
        }

        public async Task<int> GetPublishedLandingPages(int idUser)
        {
            var sessionCookie = _httpContextAccessor.HttpContext.Request.Cookies[MVC_SESSION_COOKIE_NAME] ?? throw new SessionExpiredException();

            string checkPublishedLandingsUrl = $"{_options.Value.MvcUrl}WebApp/GetLandingPagesAmount?idUser={idUser}";

            try
            {
                var landingPagesResponse = await _flurlClient
                    .Request(new UriTemplate(checkPublishedLandingsUrl).Resolve())
                    .WithCookie(MVC_SESSION_COOKIE_NAME, sessionCookie)
                    .GetJsonAsync<LandingPagesResponse>();

                return landingPagesResponse.data;
            }
            catch
            {
                throw;
            }
        }
    }
}
