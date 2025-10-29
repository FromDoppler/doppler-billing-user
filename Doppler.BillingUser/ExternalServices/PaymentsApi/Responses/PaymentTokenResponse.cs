using System.Text.Json.Serialization;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Responses
{
    public class PaymentTokenResponse
    {
        [JsonPropertyName("deregistrationLohiResponse")]
        public DeregistrationLohiResponse DeregistrationLohiResponse { get; set; }
    }

    public class DeregistrationLohiResponse
    {
        [JsonPropertyName("returnCode")]
        public string ReturnCode { get; set; }

        [JsonPropertyName("reasonCode")]
        public string ReasonCode { get; set; }

        [JsonPropertyName("returnText")]
        public string ReturnText { get; set; }

        [JsonPropertyName("encryptionTokenData")]
        public EncryptionTokenData EncryptionTokenData { get; set; }

        [JsonPropertyName("referenceTraceNumbers")]
        public ReferenceTraceNumbers ReferenceTraceNumbers { get; set; }

        [JsonPropertyName("settlementData")]
        public SettlementData SettlementData { get; set; }

        [JsonPropertyName("apiTransactionID")]
        public string ApiTransactionId { get; set; }

        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; }

        [JsonPropertyName("errorInformation")]
        public string ErrorInformation { get; set; }

        public bool IsSuccessful => ReturnCode == "0000" && ResponseCode == "000" && string.IsNullOrEmpty(ErrorInformation);
    }

    public class EncryptionTokenData
    {
        [JsonPropertyName("tokenizedPAN")]
        public string TokenizedPan { get; set; }
    }

    public class ReferenceTraceNumbers
    {
        [JsonPropertyName("systemTraceNumber")]
        public string SystemTraceNumber { get; set; }

        [JsonPropertyName("authorizationNumber")]
        public string AuthorizationNumber { get; set; }
    }

    public class SettlementData
    {
        [JsonPropertyName("settlementDate")]
        public string SettlementDate { get; set; }

        [JsonPropertyName("settlementNetwork")]
        public string SettlementNetwork { get; set; }

        [JsonPropertyName("regulationIndicator")]
        public string RegulationIndicator { get; set; }
    }
}
