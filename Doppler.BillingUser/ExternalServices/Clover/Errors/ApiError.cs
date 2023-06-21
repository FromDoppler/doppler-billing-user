namespace Doppler.BillingUser.ExternalServices.Clover.Errors
{
    public class ApiError
    {
        public string Message { get; set; }
        public ApiErrorCause Error { get; set; }

    }

    public class ApiErrorCause
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
