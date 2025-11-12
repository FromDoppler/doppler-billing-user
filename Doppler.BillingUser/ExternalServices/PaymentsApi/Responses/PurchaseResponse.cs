namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Responses
{
    public class PurchaseResponse
    {
        public CreditPurchaseResponse CreditPurchaseResponse { get; set; }
    }

    public class CreditPurchaseResponse
    {
        public string ReturnCode { get; set; }
        public string ReasonCode { get; set; }
        public MiscAmountsBalances MiscAmountsBalances { get; set; }
        public CardInfo CardInfo { get; set; }
        public AddressVerificationData AddressVerificationData { get; set; }
        public CardVerificationData CardVerificationData { get; set; }
        public VisaSpecificData VisaSpecificData { get; set; }
        public ReferenceTraceNumbers ReferenceTraceNumbers { get; set; }
        public SettlementData SettlementData { get; set; }
        public WorldPayRoutingData WorldPayRoutingData { get; set; }
        public string APITransactionID { get; set; }
        public string ResponseCode { get; set; }
        public string AuthorizationSource { get; set; }

        public bool IsSuccessful => ReturnCode == "0000" && ResponseCode == "000";
    }

    public class AddressVerificationData
    {
        public string AVSResult { get; set; }
    }

    public class WorldPayRoutingData
    {
        public string DCCEligibleBin { get; set; }
        public string NetworkId { get; set; }
    }
}
