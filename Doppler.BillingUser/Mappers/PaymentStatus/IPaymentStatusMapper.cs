using Doppler.BillingUser.ApiModels;
using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Mappers.PaymentStatus
{
    public interface IPaymentStatusMapper
    {
        PaymentStatusEnum MapToPaymentStatus(MercadoPagoPaymentStatusEnum status);

        PaymentStatusEnum MapFromPaymentStatusApiEnumToPaymentStatusEnum(PaymentStatusApiEnum status);
    }
}
