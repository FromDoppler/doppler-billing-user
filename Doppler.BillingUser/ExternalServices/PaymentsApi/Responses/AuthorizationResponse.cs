using System.Text.Json.Serialization;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Responses
{
    public class AuthorizationResponse
    {
        [JsonPropertyName("creditauthresponse")]
        public CreditAuthResponse CreditAuthResponse { get; set; }
    }

    public class CreditAuthResponse
    {
        public string ReturnCode { get; set; }
        public string ReasonCode { get; set; }
        public string ReturnText { get; set; }
        public MiscAmountsBalances MiscAmountsBalances { get; set; }
        public CardInfo CardInfo { get; set; }
        public CardVerificationData CardVerificationData { get; set; }
        public EncryptionTokenData EncryptionTokenData { get; set; }
        public ProcFlagsIndicators ProcFlagsIndicators { get; set; }
        public VisaSpecificData VisaSpecificData { get; set; }
        public ReferenceTraceNumbers ReferenceTraceNumbers { get; set; }
        public SettlementData SettlementData { get; set; }
        public string ResponseCode { get; set; }

        public bool IsSuccessful => ReturnCode == "0000" && ReasonCode == "0000";
    }

    public class ProcFlagsIndicators
    {
        [JsonPropertyName("CVV2FromReg-ID")]
        public string CVV2FromRegID { get; set; }
    }
}
