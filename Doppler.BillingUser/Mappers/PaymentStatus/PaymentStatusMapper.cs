using Doppler.BillingUser.ApiModels;
using Doppler.BillingUser.Enums;
using System;
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
                PaymentStatusApiEnum.Declined => PaymentStatusEnum.DeclinedPaymentTransaction,
                _ => throw new ArgumentException($"Unexpected value `{status}`", nameof(status))
            };
        }

        public PaymentStatusApiEnum MapFromPaymentStatusEnumToPaymentStatusApiEnum(PaymentStatusEnum status)
        {
            return status switch
            {
                PaymentStatusEnum.Approved => PaymentStatusApiEnum.Approved,
                PaymentStatusEnum.Pending => PaymentStatusApiEnum.Pending,
                PaymentStatusEnum.DeclinedPaymentTransaction => PaymentStatusApiEnum.Declined,
                _ => throw new ArgumentException($"Unexpected value `{status}`", nameof(status))
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
