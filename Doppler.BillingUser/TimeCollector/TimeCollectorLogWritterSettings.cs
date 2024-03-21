using Microsoft.Extensions.Logging;
using System;

namespace Doppler.BillingUser.TimeCollector
{
    public class TimeCollectorLogWritterSettings
    {
        public TimeSpan LogPeriod { get; set; } = new(hours: 0, minutes: 15, seconds: 0);
        public TimeSpan ResetPeriod { get; set; } = new(hours: 6, minutes: 0, seconds: 0);
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}
