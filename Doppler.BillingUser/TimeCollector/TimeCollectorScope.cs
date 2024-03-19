using System;
using System.Diagnostics;

namespace Doppler.BillingUser.TimeCollector
{
    public class TimeCollectorScope : IDisposable
    {
        private readonly TimeCollectorItem _collector;
        private readonly Stopwatch _stopwatch;

        public TimeCollectorScope(TimeCollectorItem collector)
        {
            _stopwatch = Stopwatch.StartNew();
            _collector = collector;
            _collector.Start();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _collector.Stop(_stopwatch.ElapsedTicks);
        }
    }
}
