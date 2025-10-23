using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit.AddOns
{
    public class BillingCreditForTransferMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;

        private const int MexicoIva = 16;
        private const int ArgentinaIva = 21;

        public BillingCreditForTransferMapper(IBillingRepository billingRepository)
        {
            _billingRepository = billingRepository;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(decimal total, UserBillingInformation user, Model.BillingCredit currentBillingCredit, CreditCardPayment payment, BillingCreditTypeEnum billingCreditType, Promotion currentPromotion)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);
            var durationPromotion = currentPromotion?.Duration;
            var idPromotion = currentPromotion?.IdPromotion;
            var discountPromotion = currentPromotion?.DiscountPercentage;

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = null,
                CCExpMonth = null,
                CCExpYear = null,
                CCHolderFullName = null,
                CCIdentificationType = null,
                CCIdentificationNumber = null,
                CCNumber = null,
                CCVerification = null,
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                CFDIUse = user.CFDIUse,
                PaymentWay = user.PaymentWay,
                PaymentType = user.PaymentType,
                BankName = user.BankName,
                BankAccount = user.BankAccount,
                IdPromotion = idPromotion,
                PromotionDuration = durationPromotion
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
                CurrentMonthPlan = currentBillingCredit.CurrentMonthPlan,
                DiscountPlanFeePromotion = discountPromotion
            };

            buyCreditAgreement.BillingCredit.Payed = buyCreditAgreement.BillingCredit.PaymentDate != null;

            //Calculate the BillingSystem
            buyCreditAgreement.IdResponsabileBilling = CalculateBillingSystemByTransfer(user.IdBillingCountry);

            if (user.PaymentMethod == PaymentMethodEnum.TRANSF &&
                (user.IdBillingCountry == (int)CountryEnum.Mexico || user.IdBillingCountry == (int)CountryEnum.Argentina))
            {
                int iva = (user.IdBillingCountry == (int)CountryEnum.Mexico) ? MexicoIva : ArgentinaIva;
                buyCreditAgreement.BillingCredit.Taxes = Convert.ToDouble(total * iva / 100);
            }

            return buyCreditAgreement;
        }

        private int CalculateBillingSystemByTransfer(int idBillingCountry)
        {
            return idBillingCountry switch
            {
                (int)CountryEnum.Colombia => (int)ResponsabileBillingEnum.BorisMarketing,
                (int)CountryEnum.Mexico => (int)ResponsabileBillingEnum.RC,
                (int)CountryEnum.Argentina => (int)ResponsabileBillingEnum.GBBISIDE,
                _ => (int)ResponsabileBillingEnum.GBBISIDE,
            };
        }
    }
}
