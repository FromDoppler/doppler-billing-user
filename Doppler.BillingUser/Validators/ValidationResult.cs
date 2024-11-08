namespace Doppler.BillingUser.Validators
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public ValidationError Error { get; set; }
    }
}
