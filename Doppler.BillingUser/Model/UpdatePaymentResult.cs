namespace Doppler.BillingUser.Model
{
    public class UpdatePaymentResult
    {
        public bool Success { get; }
        public string Message { get; }

        private UpdatePaymentResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static UpdatePaymentResult Failed(string message)
        {
            return new UpdatePaymentResult(false, message);
        }

        public static UpdatePaymentResult Successful()
        {
            return new UpdatePaymentResult(true, "Successfully");
        }
    }
}
