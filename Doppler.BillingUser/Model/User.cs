namespace Doppler.BillingUser.Model
{
    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int IdUser { get; set; }
        public string BillingEmails { get; set; }
        public string RazonSocial { get; set; }
        public string BillingFirstName { get; set; }
        public string BillingLastName { get; set; }
        public string BillingAddress { get; set; }
        public string CityName { get; set; }
        public int IdState { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string BillingZip { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int IdConsumerType { get; set; }
        public string CUIT { get; set; }
        public bool IsCancelated { get; set; }
        public string SapProperties { get; set; }
        public bool BlockedAccountNotPayed { get; set; }
        public int PaymentMethod { get; set; }
        public int IdBillingState { get; set; }
        public string BillingCity { get; set; }
        //State
        public string StateCountryCode { get; set; }
        //BillingCredit
        public string CCIdentificationNumber { get; set; }
        public string CCIdentificationType { get; set; }
        public int IdResponsabileBilling { get; set; }
        //UserTypesPlans
        public int IdUserType { get; set; }
        //Vendor
        public bool IsInbound { get; set; }
        public string Vendor { get; set; }
        //BillingState
        public string BillingStateCountryCode { get; set; }
        public string BillingStateName { get; set; }
        public string Company { get; set; }
        public string Language { get; set; }
        public string BillingCountryName { get; set; }
        public int IdBillingCountry { get; set; }
        public int? IdClientManager { get; set; }
    }
}
