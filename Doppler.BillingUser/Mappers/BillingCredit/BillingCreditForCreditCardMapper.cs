using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit
{
    public class BillingCreditForCreditCardMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IEncryptionService _encryptionService;

        public BillingCreditForCreditCardMapper(IBillingRepository billingRepository, IEncryptionService encryptionService)
        {
            _billingRepository = billingRepository;
            _encryptionService = encryptionService;
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
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                IdPromotion = promotion?.IdPromotion,
                PromotionDuration = promotion?.Duration
            };

            DateTime now = DateTime.UtcNow;
            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = now,
                ActivationDate = now,
                Approved = true,
                Payed = true,
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
                var currentMonthPlan = (billingCreditType == BillingCreditTypeEnum.Individual_to_Subscribers) ? buyCreditAgreement.BillingCredit.Date.Day > 20 ? 0 : 1 :
                                        currentBillingCredit == null ? (buyCreditAgreement.BillingCredit.TotalMonthPlan.HasValue
                                        && buyCreditAgreement.BillingCredit.TotalMonthPlan.Value > 1 && buyCreditAgreement.BillingCredit.Date.Day > 20)
                                        ? 0 : 1 :
                                        currentBillingCredit.CurrentMonthPlan ?? 1;

                buyCreditAgreement.BillingCredit.IdDiscountPlan = agreementInformation.DiscountId != 0 ? agreementInformation.DiscountId : 1;
                buyCreditAgreement.BillingCredit.TotalMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 1;
                buyCreditAgreement.BillingCredit.CurrentMonthPlan = currentMonthPlan;
                buyCreditAgreement.BillingCredit.SubscribersQty = newUserTypePlan.SubscribersQty;
            }

            buyCreditAgreement.IdResponsabileBilling = (int)ResponsabileBillingEnum.QBL;

            return buyCreditAgreement;
        }
    }
}
