namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapSettings
    {
        public string SapBaseUrl { get; set; }
        public string SapCreateBusinessPartnerEndpoint { get; set; }
        public string SapCreateBillingRequestEndpoint { get; set; }
        public string SapCreateCreditNoteEndpoint { get; set; }
        public int TimeZoneOffset { get; set; }
    }
}
