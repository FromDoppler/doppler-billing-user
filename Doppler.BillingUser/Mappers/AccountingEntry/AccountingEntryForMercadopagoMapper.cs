using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers
{
    public class AccountingEntryForMercadopagoMapper : IAccountingEntryMapper
    {
        private IPaymentAmountHelper _paymentAmountService;
        private const string AccountingEntryTypeDescriptionInvoice = "Invoice";
        private const string AccountEntryTypeInvoice = "I";
        private const string AccountingEntryTypeDescriptionMpPayment = "MP Payment";
        private const string AccountEntryTypePayment = "P";
        private const string PaymentEntryTypePayment = "P";

        public AccountingEntryForMercadopagoMapper(IPaymentAmountHelper paymentAmountService)
        {
            _paymentAmountService = paymentAmountService;
        }

        public async Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, UserBillingInformation user, UserTypePlanInformation newPlan, CreditCardPayment payment, AccountTypeEnum accountType)
        {
            decimal rate = 1;
            decimal invoiceTaxes = 0;

            if (total != 0)
            {
                var paymentDetails = await _paymentAmountService.ConvertCurrencyAmount(CurrencyTypeEnum.UsS, CurrencyTypeEnum.sARG, total);
                rate = paymentDetails.CurrencyRate;
                invoiceTaxes = paymentDetails.Taxes;
            }
            else
            {
                payment.Status = PaymentStatusEnum.Approved;
            }

            return new AccountingEntry
            {
                IdClient = user.IdUser,
                IdCurrencyType = (int)CurrencyTypeEnum.sARG,
                CurrencyRate = rate,
                Taxes = (double)invoiceTaxes,
                Amount = total,
                Date = DateTime.UtcNow,
                Status = payment.Status,
                Source = SourceTypeHelper.SourceTypeEnumMapper(newPlan),
                AccountingTypeDescription = AccountingEntryTypeDescriptionInvoice,
                InvoiceNumber = 0,
                IdAccountType = (int)accountType,
                IdInvoiceBillingType = (int)InvoiceBillingTypeEnum.MERCADOPAGO,
                AuthorizationNumber = payment.AuthorizationNumber,
                AccountEntryType = AccountEntryTypeInvoice
            };
        }

        public async Task<AccountingEntry> MapToInvoiceAccountingEntry(decimal total, int idUser, SourceTypeEnum source, CreditCardPayment payment, AccountTypeEnum accountType)
        {
            decimal rate = 1;
            decimal invoiceTaxes = 0;

            if (total != 0)
            {
                var paymentDetails = await _paymentAmountService.ConvertCurrencyAmount(CurrencyTypeEnum.UsS, CurrencyTypeEnum.sARG, total);
                rate = paymentDetails.CurrencyRate;
                invoiceTaxes = paymentDetails.Taxes;
            }
            else
            {
                payment.Status = PaymentStatusEnum.Approved;
            }

            return new AccountingEntry
            {
                IdClient = idUser,
                IdCurrencyType = (int)CurrencyTypeEnum.sARG,
                CurrencyRate = rate,
                Taxes = (double)invoiceTaxes,
                Amount = total,
                Date = DateTime.UtcNow,
                Status = payment.Status,
                Source = source,
                AccountingTypeDescription = AccountingEntryTypeDescriptionInvoice,
                InvoiceNumber = 0,
                IdAccountType = (int)accountType,
                IdInvoiceBillingType = (int)InvoiceBillingTypeEnum.MERCADOPAGO,
                AuthorizationNumber = payment.AuthorizationNumber,
                AccountEntryType = AccountEntryTypeInvoice
            };
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
                Date = DateTime.Now,
                Source = invoiceEntry.Source,
                AccountEntryType = AccountEntryTypePayment,
                AuthorizationNumber = invoiceEntry.AuthorizationNumber,
                PaymentEntryType = PaymentEntryTypePayment,
                AccountingTypeDescription = AccountingEntryTypeDescriptionMpPayment,
                IdAccountType = invoiceEntry.IdAccountType,
                IdCurrencyType = invoiceEntry.IdCurrencyType,
                CurrencyRate = invoiceEntry.CurrencyRate,
                Taxes = invoiceEntry.Taxes,
                IdInvoiceBillingType = invoiceEntry.IdInvoiceBillingType
            });
        }
    }
}
