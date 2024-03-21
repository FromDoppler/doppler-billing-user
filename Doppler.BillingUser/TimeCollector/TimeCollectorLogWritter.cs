using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Doppler.BillingUser.TimeCollector
{
    public class TimeCollectorLogWritter
    {
        private readonly List<Timer> _timers = new();

        public TimeCollectorLogWritter(
            ILogger<TimeCollectorLogWritter> logger,
            IOptions<TimeCollectorLogWritterSettings> options,
            ITimeCollector timeCollector)
        {
            var settings = options.Value;

            _timers.Add(new Timer(_ =>
            {
                if (logger.IsEnabled(settings.LogLevel))
                {
                    logger.Log(settings.LogLevel, "=== TimeCollector: \r\n{TimeCollectorCsv}", timeCollector.GetCsv());
                }
            }, null, settings.LogPeriod, settings.LogPeriod));

            _timers.Add(new Timer(_ =>
            {
                if (logger.IsEnabled(settings.LogLevel))
                {
                    logger.Log(settings.LogLevel, "=== TimeCollector LAST: \r\n{TimeCollectorCsv}\r\n=== RESETING TimeCollector", timeCollector.GetCsv());
                }
                timeCollector.ResetCollectors();
            }, null, settings.ResetPeriod, settings.ResetPeriod));
        }

        public void Dispose()
        {
            foreach (var timer in _timers)
            {
                timer.Dispose();
            }
            _timers.Clear();
        }
    }
}
