using System;
using System.Threading.Tasks;
using Doppler.BillingUser.TimeCollector;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.BillingUser.ExternalServices.Slack
{
    public class SlackService : ISlackService
    {
        private readonly SlackSettings _slackSettings;
        private readonly IFlurlClient _flurlClient;
        private readonly ILogger _logger;
        private readonly ITimeCollector _timeCollector;

        public SlackService(
            IOptions<SlackSettings> slackSettings,
            IFlurlClientFactory flurlClientFac,
            ILogger<SlackService> logger,
            ITimeCollector timeCollector)
        {
            _slackSettings = slackSettings.Value;
            _flurlClient = flurlClientFac.Get(_slackSettings.Url);
            _logger = logger;
            _timeCollector = timeCollector;
        }

        public async Task SendNotification(string message = null)
        {
            using var _ = _timeCollector.StartScope();
            try
            {
                await _flurlClient.Request(_slackSettings.Url).PostJsonAsync(new { text = message });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error sending slack notification");
            }
        }
    }
}
