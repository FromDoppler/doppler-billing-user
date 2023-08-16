using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Doppler.BillingUser.ApiModels
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatusApiEnum
    {
        Approved,
        Pending,
        Declined,
        Failed,
        ClientFailed,
        DoNotHonor
    }
}
