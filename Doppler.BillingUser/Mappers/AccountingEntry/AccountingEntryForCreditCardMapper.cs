using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers
{
    public class AccountingEntryForCreditCardMapper : IAccountingEntryMapper
    {
        private const string AccountingEntryTypeDescriptionInvoice = "Invoice";
        private const int UserAccountType = 1;
        private const int InvoiceBillingTypeQBL = 1;
        private const string AccountEntryTypeInvoice = "I";
        private const string AccountingEntryTypeDescriptionCCPayment = "CC Payment";
        private const string AccountEntryTypePayment = "P";
        private const string PaymentEntryTypePayment = "P";

        public Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, UserTypePlanInformation newPlan, CreditCardPayment payment)
        {
            return Task.FromResult(new AccountingEntry
            {
                IdClient = user.IdUser,
                Amount = total,
                Date = DateTime.UtcNow,
                Status = payment.Status,
                Source = SourceTypeHelper.SourceTypeEnumMapper(newPlan),
                AccountingTypeDescription = AccountingEntryTypeDescriptionInvoice,
                InvoiceNumber = 0,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = InvoiceBillingTypeQBL,
                AuthorizationNumber = payment.AuthorizationNumber,
                AccountEntryType = AccountEntryTypeInvoice
            });
        }

        public Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, int idUser, SourceTypeEnum source, CreditCardPayment payment)
        {
            return Task.FromResult(new AccountingEntry
            {
                IdClient = idUser,
                Amount = total,
                Date = DateTime.UtcNow,
                Status = payment.Status,
                Source = source,
                AccountingTypeDescription = AccountingEntryTypeDescriptionInvoice,
                InvoiceNumber = 0,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = InvoiceBillingTypeQBL,
                AuthorizationNumber = payment.AuthorizationNumber,
                AccountEntryType = AccountEntryTypeInvoice
            });
        }

        public Task<AccountingEntry> MapToPaymentAccountingEntry(AccountingEntry invoiceEntry, CreditCard encryptedCreditCard)
        {
            return Task.FromResult(new AccountingEntry
            {
                IdClient = invoiceEntry.IdClient,
                Amount = invoiceEntry.Amount,
                CcCNumber = encryptedCreditCard.Number,
                CcExpMonth = encryptedCreditCard.ExpirationMonth,
                CcExpYear = encryptedCreditCard.ExpirationYear,
                CcHolderName = encryptedCreditCard.HolderName,
                Date = DateTime.UtcNow,
                Source = invoiceEntry.Source,
                AccountingTypeDescription = AccountingEntryTypeDescriptionCCPayment,
                IdAccountType = UserAccountType,
                IdInvoiceBillingType = InvoiceBillingTypeQBL,
                AccountEntryType = AccountEntryTypePayment,
                AuthorizationNumber = invoiceEntry.AuthorizationNumber,
                PaymentEntryType = PaymentEntryTypePayment
            });
        }
    }
}
