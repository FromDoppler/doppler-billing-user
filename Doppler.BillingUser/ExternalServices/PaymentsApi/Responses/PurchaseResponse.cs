namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Responses
{
    public class PurchaseResponse
    {
        public string AuthorizationNumber { get; set; } = null!;

        public string ResponseCode { get; set; }
        public bool IsSuccessful => ResponseCode == "000";

        //public CreditPurchaseResponse CreditPurchaseResponse { get; set; }
    }

    //    public VisaSpecificData VisaSpecificData { get; set; }
    //    public ReferenceTraceNumbers ReferenceTraceNumbers { get; set; }
    //    public SettlementData SettlementData { get; set; }
    //    public WorldPayRoutingData WorldPayRoutingData { get; set; }

    //{
    //    public string AVSResult { get; set; }
    //}
    //public class CardVerificationData
    //{
    //    public string Cvv2Cvc2CIDResult { get; set; }
    //}
    //public class VisaSpecificData
    //{
    //    public string VisaSpendQualifier { get; set; }
    //    public string VisaAccountType { get; set; }
    //    public string VisaResponseCode { get; set; }
    //    public string VisaValidationCode { get; set; }
    //    public string VisaCardLevelResults { get; set; }
    //    public string VisaTransactionId { get; set; }
    //    public string VisaAuthCharId { get; set; }
    //    public string VisaAccountFundingSource { get; set; }
    //}
    //public class WorldPayRoutingData
    //{
    //    public string DCCEligibleBin { get; set; }
    //    public string NetworkId { get; set; }
    //}
}
