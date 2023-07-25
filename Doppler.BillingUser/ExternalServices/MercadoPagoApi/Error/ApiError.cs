namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi.Error
{
    public class ApiError
    {
        public int? Status { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }
}
