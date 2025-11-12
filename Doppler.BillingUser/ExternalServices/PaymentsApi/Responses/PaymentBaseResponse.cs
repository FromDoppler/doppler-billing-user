using System.Text.Json.Serialization;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Responses
{
    public class MiscAmountsBalances
    {
        public string AvailableBALFromAcct { get; set; }
    }

    public class CardInfo
    {
        public string CardProductCode { get; set; }
    }

    public class CardVerificationData
    {
        public string Cvv2Cvc2CIDResult { get; set; }
    }

    public class EncryptionTokenData
    {
        [JsonPropertyName("tokenizedPAN")]
        public string TokenizedPan { get; set; }
    }

    public class VisaSpecificData
    {
        public string VisaSpendQualifier { get; set; }
        public string VisaAccountType { get; set; }
        public string VisaResponseCode { get; set; }
        public string VisaValidationCode { get; set; }
        public string VisaCardLevelResults { get; set; }
        public string VisaTransactionId { get; set; }
        public string VisaAuthCharId { get; set; }
        public string VisaAccountFundingSource { get; set; }
    }

    public class ReferenceTraceNumbers
    {
        [JsonPropertyName("retrievalREFNumber")]
        public string RetrievalREFNumber { get; set; }

        [JsonPropertyName("paymentAcctREFNumber")]
        public string PaymentAcctREFNumber { get; set; }

        [JsonPropertyName("networkRefNumber")]
        public string NetworkRefNumber { get; set; }

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
