using System;

namespace Doppler.BillingUser.Utils
{
    public static class ConsumerTypeHelper
    {
        public static string GetConsumerType(string idConsumerType)
        {
            var isConsumerTypeValid = Enum.TryParse<ConsumerType>(idConsumerType, out var consumer);

            return !isConsumerTypeValid ? null : consumer.ToString();
        }
    }
}
