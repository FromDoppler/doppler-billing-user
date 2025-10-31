using Doppler.BillingUser.Enums;
using System;

namespace Doppler.BillingUser.ExternalServices.PaymentsApi.Exceptions
{
    public class PaymentException()
    {
        public PaymentErrorCode ErrorCode { get; }
        public PaymentError PaymentError { get; }
    }
}
