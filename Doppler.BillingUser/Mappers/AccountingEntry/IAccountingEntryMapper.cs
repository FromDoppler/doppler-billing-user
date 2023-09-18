using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers
{
    public interface IAccountingEntryMapper
    {
        Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, UserTypePlanInformation newPlan, CreditCardPayment payment);

        Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, int idUser, SourceTypeEnum source, CreditCardPayment payment);
        Task<AccountingEntry> MapToPaymentAccountingEntry(AccountingEntry invoiceEntry, CreditCard encryptedCreditCard);

    }
}
