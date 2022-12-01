using Doppler.BillingUser.ApiModels;
using Doppler.BillingUser.Enums;
using System;
using System.Collections;

namespace Doppler.BillingUser.Mappers.PaymentStatus
{
    public static class PaymentStatusMapper
    {
        public static PaymentStatusEnum MapToPaymentStatusEnum(this PaymentStatusApiEnum status)
        {
            return status switch
            {
                PaymentStatusApiEnum.Approved => PaymentStatusEnum.Approved,
                PaymentStatusApiEnum.Pending => PaymentStatusEnum.Pending,
                PaymentStatusApiEnum.Declined => PaymentStatusEnum.DeclinedPaymentTransaction,
                _ => throw new ArgumentException($"Unexpected value `{status}`", nameof(status))
            };
        }

        public static PaymentStatusApiEnum MapToPaymentStatusApiEnum(this PaymentStatusEnum status)
        {
            return status switch
            {
                PaymentStatusEnum.Approved => PaymentStatusApiEnum.Approved,
                PaymentStatusEnum.Pending => PaymentStatusApiEnum.Pending,
                PaymentStatusEnum.DeclinedPaymentTransaction => PaymentStatusApiEnum.Declined,
                _ => throw new ArgumentException($"Unexpected value `{status}`", nameof(status))
            };
        }

        public static PaymentStatusEnum MapToPaymentStatus(this MercadoPagoPaymentStatusEnum status)
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
