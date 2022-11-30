using Doppler.BillingUser.ApiModels;
using Doppler.BillingUser.Enums;
using System.Collections;

namespace Doppler.BillingUser.Mappers.PaymentStatus
{
    public class PaymentStatusMapper : IPaymentStatusMapper
    {
        public PaymentStatusEnum MapFromPaymentStatusApiEnumToPaymentStatusEnum(PaymentStatusApiEnum status)
        {
            return status switch
            {
                PaymentStatusApiEnum.Approved => PaymentStatusEnum.Approved,
                PaymentStatusApiEnum.Pending => PaymentStatusEnum.Pending,
                PaymentStatusApiEnum.Declined => PaymentStatusEnum.DeclinedPaymentTransaction
            };
        }

        public PaymentStatusEnum MapToPaymentStatus(MercadoPagoPaymentStatusEnum status)
        {
            return status switch
            {
                MercadoPagoPaymentStatusEnum.Approved or MercadoPagoPaymentStatusEnum.Authorized => PaymentStatusEnum.Approved,
                MercadoPagoPaymentStatusEnum.In_Mediation or MercadoPagoPaymentStatusEnum.In_Process or MercadoPagoPaymentStatusEnum.Pending => PaymentStatusEnum.Pending,
                _ => PaymentStatusEnum.DeclinedPaymentTransaction,
            };
        }
    }
}
