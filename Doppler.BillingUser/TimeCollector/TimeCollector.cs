using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System;
using System.Globalization;

namespace Doppler.BillingUser.TimeCollector
{
    public class TimeCollector : ITimeCollector
    {
        private Dictionary<string, TimeCollectorItem> _collectorsByDifferentiator = new Dictionary<string, TimeCollectorItem>();
        private readonly object lockObject = new object();

        private string BuildDifferentiator(string extraId, string filePath, int lineNumber, string memberName)
        {
            var baseDifferentiator = PrefixRemovalRegex.Replace(filePath, string.Empty);
            return extraId == null
                ? string.Format("{0}[{1}]#{2}", baseDifferentiator, memberName, lineNumber)
                : string.Format("{0}[{1}][{2}]#{3}", baseDifferentiator, memberName, extraId, lineNumber);
        }

        public IDisposable StartScope(
            string extraId = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = null)
        {
            return StartScopeCustom(BuildDifferentiator(extraId, filePath, lineNumber, memberName));
        }

        public IDisposable StartScopeCustom(string differentiator)
        {
            var collector = GetByDifferentiator(differentiator);
            return new TimeCollectorScope(collector);
        }

        public void CountException(
            Exception exception,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = null)
        {
            var exceptionName = exception.InnerException == null ? exception.GetType().Name
                : string.Format("{0}_{1}", exception.GetType().Name, exception.InnerException.GetType().Name);

            CountEventCustom(BuildDifferentiator(exceptionName, filePath, lineNumber, memberName));
        }

        public void CountEvent(
            string extraId = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = null)
        {
            CountEventCustom(BuildDifferentiator(extraId, filePath, lineNumber, memberName));
        }

        public void CountEventCustom(string differentiator)
        {
            GetByDifferentiator(differentiator).Increment();
        }

        private TimeCollectorItem GetByDifferentiator(string differentiator)
        {
            TimeCollectorItem collector;
            if (!_collectorsByDifferentiator.TryGetValue(differentiator, out collector))
            {
                lock (lockObject)
                {
                    if (!_collectorsByDifferentiator.TryGetValue(differentiator, out collector))
                    {
                        collector = new TimeCollectorItem(differentiator);
                        _collectorsByDifferentiator[differentiator] = collector;
                    }
                }
            }
            return collector;
        }

        public void ResetCollectors()
        {
            lock (lockObject)
            {
                _collectorsByDifferentiator = new Dictionary<string, TimeCollectorItem>();
            }
        }

        public string GetCsv()
        {
            TimeCollectorItem[] values;
            lock (lockObject)
            {
                values = _collectorsByDifferentiator.Values.ToArray();
            }

            return string.Join("\r\n",
                new[] { "Differentiator,CreatedAt,LiveTime,ExecutedTime,ExecutedCount,CurrentExecutingCount,TicksAvg,TicksMin,TicksMax" }
                .Union(values.Select(x =>
                    String.Format(CultureInfo.InvariantCulture, "{0},{1:yyyy-MM-dd HH:mm:ss.fff},{2:c},{3:c},{4},{5},{6},{7},{8}",
                    x.Differentiator, x.CreatedAt, x.LiveTime, x.ExecutedTime, x.ExecutedCount, x.CurrentExecutingCount, x.TicksAvg, x.TicksMin, x.TicksMax))));
        }

        #region Ugly hack to remove project path
        // https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.callerfilepathattribute(v=vs.110).aspx
        // CallerFilePath includes file path, but we do not need it
        // and it depends on who build the project, so this hack is
        // made to remove it.
        private static Regex PrefixRemovalRegex;
        static TimeCollector()
        {
            UpdatePrefixRemovalRegex();
        }
        static void UpdatePrefixRemovalRegex([CallerFilePath] string filePath = null)
        {
            var notPrefix = new Regex(@"Doppler\.Transversal\\TimeCollector\\TimeCollector\.cs$");
            var unusefulPrefix = notPrefix.Replace(filePath, string.Empty);
            PrefixRemovalRegex = new Regex(string.Format("^{0}", Regex.Escape(unusefulPrefix)));
        }
        #endregion
    }
}
