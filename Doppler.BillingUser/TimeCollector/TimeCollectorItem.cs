using System.Diagnostics;
using System.Threading;
using System;

namespace Doppler.BillingUser.TimeCollector
{
    public class TimeCollectorItem
    {
        private int _currentExecutingCount = 0;
        private long _executedCount = 0;
        private long _rawAggregatedTicks = 0;
        private long? _rawMinTicks = null;
        private long? _rawMaxTicks = null;

        public string Differentiator { get; }
        public DateTime CreatedAt { get; }
        public TimeSpan LiveTime
        {
            get
            {
                return DateTime.UtcNow - CreatedAt;
            }
        }
        public TimeSpan ExecutedTime
        {
            get
            {
                return new TimeSpan(ConvertFromStopwatchToTimespanTicks(_rawAggregatedTicks));
            }
        }

        public long? TicksAvg
        {
            get
            {
                return _rawAggregatedTicks > 0 ? (long?)ConvertFromStopwatchToTimespanTicks(_rawAggregatedTicks) / _executedCount : null;
            }
        }

        public long? TicksMin
        {
            get
            {
                return _rawMinTicks.HasValue ? (long?)ConvertFromStopwatchToTimespanTicks(_rawMinTicks.Value) : null;
            }
        }

        public long? TicksMax
        {
            get
            {
                return _rawMaxTicks.HasValue ? (long?)ConvertFromStopwatchToTimespanTicks(_rawMaxTicks.Value) : null;
            }
        }

        public long ExecutedCount
        {
            get
            {
                return _executedCount;
            }
        }

        public int CurrentExecutingCount
        {
            get
            {
                return _currentExecutingCount;
            }
        }

        public TimeCollectorItem(string differentiator)
        {
            Differentiator = differentiator;
            CreatedAt = DateTime.UtcNow;
        }

        public void Start()
        {
            Interlocked.Increment(ref _currentExecutingCount);
        }

        public void Stop(long elapsedStopwatchTicks)
        {
            Interlocked.Decrement(ref _currentExecutingCount);
            Interlocked.Increment(ref _executedCount);
            Interlocked.Add(ref _rawAggregatedTicks, elapsedStopwatchTicks);

            // TODO: use thread safe operations for min and max
            if (_rawMinTicks == null || _rawMinTicks > elapsedStopwatchTicks)
            {
                _rawMinTicks = elapsedStopwatchTicks;
            }
            if (_rawMaxTicks == null || _rawMaxTicks < elapsedStopwatchTicks)
            {
                _rawMaxTicks = elapsedStopwatchTicks;
            }
        }

        public void Increment()
        {
            Interlocked.Increment(ref _executedCount);
        }

        private static readonly double StopwatchTimespanTicksRelation = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public static long ConvertFromStopwatchToTimespanTicks(long stopwatchTicks)
        {
            return (long)(stopwatchTicks * StopwatchTimespanTicksRelation);
        }
    }
}
