namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Responses
{
    public class PurchaseResponse
    {
        public string AuthorizationNumber { get; set; } = null!;
        public string ResponseCode { get; set; }
        public bool IsSuccessful => ResponseCode == "000";

        //public CreditPurchaseResponse CreditPurchaseResponse { get; set; }
    }
    //public class CreditPurchaseResponse
    //{
    //    public string ReturnCode { get; set; }
    //    public string ReasonCode { get; set; }
    //    public MiscAmountsBalances MiscAmountsBalances { get; set; }
    //    public CardInfo CardInfo { get; set; }
    //    public AddressVerificationData AddressVerificationData { get; set; }
    //    public CardVerificationData CardVerificationData { get; set; }
    //    public VisaSpecificData VisaSpecificData { get; set; }
    //    public ReferenceTraceNumbers ReferenceTraceNumbers { get; set; }
    //    public SettlementData SettlementData { get; set; }
    //    public WorldPayRoutingData WorldPayRoutingData { get; set; }
    //    public string APITransactionID { get; set; }
    //    public string ResponseCode { get; set; }
    //    public string AuthorizationSource { get; set; }

    //    public bool IsSuccessful => ReturnCode == "0000" && ResponseCode == "000";
    //}
    //public class MiscAmountsBalances
    //{
    //    public string AvailableBALFromAcct { get; set; }
    //}
    //public class CardInfo
    //{
    //    public string CardProductCode { get; set; }
    //}
    //public class AddressVerificationData
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
