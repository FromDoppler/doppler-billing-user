namespace Doppler.BillingUser.ExternalServices.BinApi.Responses
{
    public class BankIdentificationNumberResponse
    {
        public string Number { get; set; }
        public string Scheme { get; set; }
        public string Type { get; set; }
        public string Currency { get; set; }
        public string Country { get; set; }
        public string Level { get; set; }
        public bool IsPrepaid { get; set; }
        public string Issuer { get; set; }
    }
}
