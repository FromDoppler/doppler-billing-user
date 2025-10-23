using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit.AddOns
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

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(decimal total, UserBillingInformation user, Model.BillingCredit currentBillingCredit, CreditCardPayment payment, BillingCreditTypeEnum billingCreditType, Promotion currentPromotion)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);
            var durationPromotion = currentPromotion?.Duration;
            var idPromotion = currentPromotion?.IdPromotion;
            var discountPromotion = currentPromotion?.DiscountPercentage;

            var creditCardData = new
            {
                currentPaymentMethod.IdCCType,
                CCExpMonth = !string.IsNullOrEmpty(currentPaymentMethod.CCExpMonth) ? short.Parse(currentPaymentMethod.CCExpMonth) : (short?)null,
                CCExpYear = !string.IsNullOrEmpty(currentPaymentMethod.CCExpYear) ? short.Parse(currentPaymentMethod.CCExpYear) : (short?)null,
                CCIdentificationNumber = !string.IsNullOrEmpty(currentPaymentMethod.CCNumber) ? CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(currentPaymentMethod.CCNumber)) : string.Empty,
                currentPaymentMethod.CCHolderFullName,
                currentPaymentMethod.CCType,
                currentPaymentMethod.CCVerification,
                currentPaymentMethod.CCNumber,
            };

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = creditCardData.IdCCType,
                CCExpMonth = creditCardData.CCExpMonth,
                CCExpYear = creditCardData.CCExpYear,
                CCHolderFullName = creditCardData.CCHolderFullName,
                CCIdentificationType = creditCardData.CCType,
                CCIdentificationNumber = creditCardData.CCIdentificationNumber,
                CCNumber = creditCardData.CCNumber,
                CCVerification = creditCardData.CCVerification,
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                IdResponsabileBilling = (int)ResponsabileBillingEnum.QBL,
                IdPromotion = idPromotion,
                PromotionDuration = durationPromotion
            };

            DateTime now = DateTime.UtcNow;
            buyCreditAgreement.BillingCredit = new BillingCreditModel
            {
                Date = now,
                PaymentDate = now,
                ActivationDate = now,
                Approved = true,
                Payed = true,
                PlanFee = (double)total,
                IdBillingCreditType = (int)billingCreditType,
                TotalMonthPlan = currentBillingCredit.TotalMonthPlan,
                IdDiscountPlan = currentBillingCredit.IdDiscountPlan,
                CurrentMonthPlan = currentBillingCredit.CurrentMonthPlan,
                DiscountPlanFeePromotion = discountPromotion
            };

            return buyCreditAgreement;
        }
    }
}
