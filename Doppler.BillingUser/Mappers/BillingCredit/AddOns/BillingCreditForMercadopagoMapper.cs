using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit.AddOns
{
    public class BillingCreditForMercadopagoMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IPaymentAmountHelper _paymentAmountService;
        private const int CF = 1;

        public BillingCreditForMercadopagoMapper(IBillingRepository billingRepository, IEncryptionService encryptionService, IPaymentAmountHelper paymentAmountService)
        {
            _billingRepository = billingRepository;
            _encryptionService = encryptionService;
            _paymentAmountService = paymentAmountService;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(decimal total, UserBillingInformation user, Model.BillingCredit currentBillingCredit, CreditCardPayment payment, BillingCreditTypeEnum billingCreditType)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = currentPaymentMethod.IdCCType,
                CCExpMonth = short.Parse(currentPaymentMethod.CCExpMonth),
                CCExpYear = short.Parse(currentPaymentMethod.CCExpYear),
                CCHolderFullName = currentPaymentMethod.CCHolderFullName,
                CCIdentificationType = currentPaymentMethod.CCType,
                CCIdentificationNumber = CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(currentPaymentMethod.CCNumber)),
                CCNumber = currentPaymentMethod.CCNumber,
                CCVerification = currentPaymentMethod.CCVerification,
                IdConsumerType = CF,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                IdResponsabileBilling = (int)ResponsabileBillingEnum.Mercadopago
            };

            DateTime now = DateTime.UtcNow;
            var isUpgradePending = BillingHelper.IsUpgradePending(user, null, payment);

            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = (billingCreditType == BillingCreditTypeEnum.Landing_Request ||
                                billingCreditType == BillingCreditTypeEnum.Conversation_Request ||
                                billingCreditType == BillingCreditTypeEnum.OnSite_Request ||
                                billingCreditType == BillingCreditTypeEnum.PushNotification_Request) ? !isUpgradePending ? now : null : now,
                ActivationDate = (billingCreditType == BillingCreditTypeEnum.Landing_Request ||
                                billingCreditType == BillingCreditTypeEnum.Conversation_Request ||
                                billingCreditType == BillingCreditTypeEnum.OnSite_Request ||
                                billingCreditType == BillingCreditTypeEnum.PushNotification_Request) ? !isUpgradePending ? now : null : now,
                Approved = (billingCreditType != BillingCreditTypeEnum.Landing_Request &&
                            billingCreditType != BillingCreditTypeEnum.Conversation_Request &&
                            billingCreditType != BillingCreditTypeEnum.OnSite_Request &&
                            billingCreditType != BillingCreditTypeEnum.PushNotification_Request) || !isUpgradePending,
                PlanFee = (double)total,
                IdBillingCreditType = (int)billingCreditType,
                TotalMonthPlan = currentBillingCredit.TotalMonthPlan,
                IdDiscountPlan = currentBillingCredit.IdDiscountPlan,
                CurrentMonthPlan = currentBillingCredit.CurrentMonthPlan
            };

            buyCreditAgreement.BillingCredit.Payed = buyCreditAgreement.BillingCredit.PaymentDate != null;

            if (total != 0)
            {
                var amountDetails = await _paymentAmountService.ConvertCurrencyAmount(CurrencyTypeEnum.UsS, CurrencyTypeEnum.sARG, total);
                buyCreditAgreement.BillingCredit.Taxes = (double)amountDetails.Taxes;
            }

            return buyCreditAgreement;
        }
    }
}
