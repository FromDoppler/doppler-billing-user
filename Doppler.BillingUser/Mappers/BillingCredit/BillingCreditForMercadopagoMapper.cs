using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit
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

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(AgreementInformation agreementInformation, UserBillingInformation user, UserTypePlanInformation newUserTypePlan, Promotion promotion, CreditCardPayment payment, Model.BillingCredit currentBillingCredit, BillingCreditTypeEnum billingCreditType)
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
                IdPromotion = promotion?.IdPromotion,
                PromotionDuration = promotion?.Duration
            };

            DateTime now = DateTime.UtcNow;
            var isUpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);

            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = !isUpgradePending ? now : null,
                ActivationDate = !isUpgradePending ? now : null,
                Approved = !isUpgradePending,
                Payed = !isUpgradePending,
                IdUserTypePlan = newUserTypePlan.IdUserTypePlan,
                PlanFee = newUserTypePlan.Fee,
                CreditsQty = newUserTypePlan.EmailQty ?? null,
                ExtraEmailFee = newUserTypePlan.ExtraEmailCost ?? null,
                ExtraCreditsPromotion = promotion?.ExtraCredits,
                DiscountPlanFeePromotion = promotion?.DiscountPercentage,
                IdBillingCreditType = (int)billingCreditType
            };

            if (newUserTypePlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
            {
                var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(agreementInformation.DiscountId);

                buyCreditAgreement.BillingCredit.TotalMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 1;

                var currentMonthPlan = (billingCreditType == BillingCreditTypeEnum.Individual_to_Subscribers) ? buyCreditAgreement.BillingCredit.Date.Day > 20 ? 0 : 1 :
                                        currentBillingCredit == null ? (buyCreditAgreement.BillingCredit.TotalMonthPlan.HasValue
                                        && buyCreditAgreement.BillingCredit.TotalMonthPlan.Value > 1 && buyCreditAgreement.BillingCredit.Date.Day > 20)
                                        ? 0 : 1 :
                                        currentBillingCredit.CurrentMonthPlan ?? 1;

                buyCreditAgreement.BillingCredit.IdDiscountPlan = agreementInformation.DiscountId != 0 ? agreementInformation.DiscountId : 1;
                buyCreditAgreement.BillingCredit.CurrentMonthPlan = currentMonthPlan;
                buyCreditAgreement.BillingCredit.SubscribersQty = newUserTypePlan.SubscribersQty;
            }

            if (agreementInformation.Total != 0)
            {
                var amountDetails = await _paymentAmountService.ConvertCurrencyAmount(CurrencyTypeEnum.UsS, CurrencyTypeEnum.sARG, agreementInformation.Total.Value);
                buyCreditAgreement.BillingCredit.Taxes = (double)amountDetails.Taxes;
            }


            buyCreditAgreement.IdResponsabileBilling = (int)ResponsabileBillingEnum.Mercadopago;

            return buyCreditAgreement;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(UserBillingInformation user, Model.BillingCredit currentBillingCredit)
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
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentBillingCredit.Cuit,
                IdPromotion = currentBillingCredit.IdPromotion,
                PromotionDuration = currentBillingCredit.PromotionDuration
            };

            DateTime now = DateTime.UtcNow;
            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = now,
                ActivationDate = now,
                Approved = true,
                Payed = true,
                IdUserTypePlan = currentBillingCredit.IdUserTypePlan,
                PlanFee = (double?)currentBillingCredit.PlanFee,
                CreditsQty = currentBillingCredit.CreditsQty,
                ExtraEmailFee = currentBillingCredit.ExtraEmailFee,
                ExtraCreditsPromotion = currentBillingCredit.ExtraCreditsPromotion,
                DiscountPlanFeePromotion = currentBillingCredit.DiscountPlanFeePromotion,
                IdBillingCreditType = (int)currentBillingCredit.IdBillingCreditType
            };

            if (currentBillingCredit.IdUserType == (int)UserTypeEnum.SUBSCRIBERS)
            {
                buyCreditAgreement.BillingCredit.TotalMonthPlan = currentBillingCredit.TotalMonthPlan;
                buyCreditAgreement.BillingCredit.IdDiscountPlan = currentBillingCredit.IdDiscountPlan;
                buyCreditAgreement.BillingCredit.CurrentMonthPlan = currentBillingCredit.CurrentMonthPlan;
                buyCreditAgreement.BillingCredit.SubscribersQty = currentBillingCredit.SubscribersQty;
            }

            buyCreditAgreement.IdResponsabileBilling = (int)ResponsabileBillingEnum.Mercadopago;

            return buyCreditAgreement;
        }
    }
}
