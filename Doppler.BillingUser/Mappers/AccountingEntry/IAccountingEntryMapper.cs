using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers
{
    public interface IAccountingEntryMapper
    {
        Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, UserTypePlanInformation newPlan, CreditCardPayment payment, AccountTypeEnum accountType);

        Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, int idUser, SourceTypeEnum source, CreditCardPayment payment, AccountTypeEnum accountType);
        Task<AccountingEntry> MapToPaymentAccountingEntry(AccountingEntry invoiceEntry, CreditCard encryptedCreditCard);

    }
}
