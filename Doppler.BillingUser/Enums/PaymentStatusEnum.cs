using System.ComponentModel;

namespace Doppler.BillingUser.Enums
{
    public enum PaymentStatusEnum
    {
        [Description("Approved")]
        Approved,
        [Description("Pending")]
        Pending,
        [Description("Declined")]
        DeclinedPaymentTransaction,
        [Description("FailedPaymentTransaction")]
        FailedPaymentTransaction,
        [Description("Credit Note Generated")]
        CreditNoteGenerated,
        [Description("ClientPaymentTransactionError")]
        ClientPaymentTransactionError,
        [Description("DoNotHonorPaymentResponse")]
        DoNotHonorPaymentResponse,
        [Description("MercadopagoCardException")]
        MercadopagoCardException
    }
}
