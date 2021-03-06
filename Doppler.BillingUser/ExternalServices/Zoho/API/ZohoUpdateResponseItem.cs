using Newtonsoft.Json;

namespace Doppler.BillingUser.ExternalServices.Zoho.API
{
    public class ZohoUpdateResponseItem
    {
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "details")]
        public ZohoUpdateResponseDetail Details { get; set; }
    }
}
